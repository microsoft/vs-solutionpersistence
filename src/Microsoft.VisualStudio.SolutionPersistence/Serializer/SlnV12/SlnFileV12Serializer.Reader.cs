// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Utilities;

namespace Microsoft.VisualStudio.SolutionPersistence.Serializer.SlnV12;

internal sealed partial class SlnFileV12Serializer
{
    /// <summary>
    /// Reads Format 12.0 solution file and creates <see cref="SolutionModel" /> out of it.
    /// Solution file line parser.
    /// Differences with original solution parser:
    /// 1) It builds solution file model, instead of immediately create Solution objects.
    /// 2) It is versy slightly relaxed, to avoid cases it would reject a solution content when it is hard to tell the reason why just looking at content.
    ///    This is mainly some inconsistnet arbitrary "erratas" around the spaces placement. (most of this cases were most likely bugs).
    /// it is a single scan forward parser (each line should be scanned only once).
    /// </summary>
    internal ref struct Reader(StreamReader reader, string? fullPath)
    {
        private enum LineType
        {
            Project,
            ProjectSection,
            EndProjectSection,
            EndProject,

            Global,
            GlobalSection,
            EndGlobalSection,
            EndGlobal,

            VisualStudioVersion,
            MinimumVisualStudioVersion,
            CommentLine,
            CommentLineEx, // extended [..#] or [..#.xxx]
            Empty,
            Property,
        }

        private int lineNumber = 0;

        private bool corrupted = false;

        public ValueTask<SolutionModel> ParseAsync(ISolutionSerializer serializer, string? fullPath, CancellationToken cancellationToken)
        {
            string? vsVersion = null;
            string? minVsVersion = null;
            string? openWithVsVersion = null; // VS version that saved last.

            SolutionItemModel? currentProject = null;
            SolutionPropertyBag? currentPropertyBag = null;

            bool inProject = false;
            bool inProjectSection = false;

            bool inGlobal = false;
            bool inGlobalSection = false;

            SolutionModel solutionModel = new SolutionModel();

            if (!this.TryParseFormatLine())
            {
                this.OnParseError(ParseError.NotASln12File);
            }

            // Some property bags need to be loaded after all projects have been resolved.
            List<(SolutionItemModel, SolutionPropertyBag)> delayLoadProperties = [];

            while (this.ReadLine(out StringTokenizer tokenizer))
            {
                cancellationToken.ThrowIfCancellationRequested();

                LineType lineType = GetLineType(ref tokenizer, allowSolutionProperties: !(inProject || inGlobal));

                // there are many legacy errata issues with parsing the solution file
                // where we accept a lot of "bad/illogical" formatting, and be very strict in other.
                // generally solution parser will accept large number of logically invalid formats.
                switch (lineType)
                {
                    case LineType.Project:
                        _ = this.Validate(!inProject);
                        inProject = true;
                        currentProject = this.ReadProjectInfo(solutionModel, ref tokenizer);
                        break;

                    case LineType.EndProject:
                        _ = this.Validate(inProject);
                        inProject = false;
                        AddProjectProperties(currentProject, currentPropertyBag, delayLoadProperties);
                        currentPropertyBag = null;
                        currentProject = null;
                        break;

                    case LineType.Global:
                        _ = this.Validate(!inProject);
                        inGlobal = true;
                        break;

                    case LineType.EndGlobal:
                        _ = this.Validate(inGlobal);
                        inGlobal = false;
                        solutionModel.AddSlnProperties(currentPropertyBag);
                        currentPropertyBag = null;
                        break;

                    case LineType.ProjectSection:
                        _ = this.Validate(inProject);
                        inProjectSection = true;
                        bool checkOnly = currentProject is null;
                        currentPropertyBag = this.ReadPropertyBag(ref tokenizer, isSolution: false, checkOnly);
                        break;

                    case LineType.EndProjectSection:
                        _ = this.Validate(inProject && inProjectSection);
                        inProjectSection = false;
                        AddProjectProperties(currentProject, currentPropertyBag, delayLoadProperties);
                        currentPropertyBag = null;
                        break;

                    case LineType.GlobalSection:
                        _ = this.Validate(inGlobal);
                        inGlobalSection = true;
                        currentPropertyBag = this.ReadPropertyBag(ref tokenizer, isSolution: true, checkOnly: false);
                        break;

                    case LineType.EndGlobalSection:
                        _ = this.Validate(inGlobal && inGlobalSection);
                        inGlobalSection = false;
                        solutionModel.AddSlnProperties(currentPropertyBag);
                        currentPropertyBag = null;
                        break;

                    case LineType.VisualStudioVersion:
                        // we differ here a little bit. The original parser was very strict here, while it is very inconsequential optional values.
                        vsVersion = tokenizer.NextToken(SlnConstants.VersionSeparators).ToString();
                        break;

                    case LineType.MinimumVisualStudioVersion:
                        minVsVersion = tokenizer.NextToken(SlnConstants.VersionSeparators).ToString();
                        break;

                    case LineType.CommentLine:
                        // oddly for valid <Global> first solution this will still work
                        if (openWithVsVersion is null && solutionModel.SolutionProjects.Count == 0)
                        {
                            openWithVsVersion = tokenizer.StringLine;
                        }

                        break;

                    case LineType.Property:
                        if (!this.Validate(currentPropertyBag))
                        {
                            break;
                        }

                        StringSpan propName = tokenizer.NextToken('=');

                        // note intentionally more relaxed than original parse.
                        // first it accepts spaces at the start and tabs at the end, and also will not require exactly 2 tabs at start and exactly 1 space at the end.
                        // original will load the solution as well, but will mark it as "corrupt" with implication to "isDirty" and such.
                        propName = propName.Trim();

                        // similar for values
                        tokenizer.TrimStart();
                        StringSpan propValue = tokenizer.Current;

                        // note: it does not strip trailing spaces for value. That was obvious bug, but in fact some could exploited it to store spaces at the end of values.
                        // tokenizer.Trim(ref value);

                        // NOTE: The value can be an empty string.
                        string propNameString = propName.ToString();
                        currentPropertyBag.Add(propNameString, propValue.ToString());
                        break;

                    case LineType.Empty:
                    case LineType.CommentLineEx:
                        break;

                    default:
                        _ = this.Validate(false);
                        break;
                }
            }

            // The project dependencies properties require the projects to all be loaded,
            // so they are processed after the model has added all of the projects.
            foreach ((SolutionItemModel item, SolutionPropertyBag properties) in delayLoadProperties)
            {
                item.AddSlnProperties(properties);
            }

            string? openWithVS = CommentToOpenWithVS(openWithVsVersion.AsSpan());
            if (!openWithVS.IsNullOrEmpty())
            {
                solutionModel.SetOpenWithVisualStudio(openWithVS);
            }

            solutionModel.MinVsVersion = minVsVersion;
            solutionModel.VsVersion = vsVersion;
            solutionModel.SerializerExtension = new SlnV12ModelExtension(
                serializer,
                new SlnV12SerializerSettings() { Encoding = GetSlnFileEncoding(reader) },
                fullPath)
            { Corrupted = this.corrupted };
            return new ValueTask<SolutionModel>(solutionModel);

            static void AddProjectProperties(
                SolutionItemModel? currentProject,
                SolutionPropertyBag? currentPropertyBag,
                List<(SolutionItemModel, SolutionPropertyBag)> delayLoadProperties)
            {
                if (currentProject is null || currentPropertyBag is null)
                {
                    return;
                }

                if (SectionName.InternKnownSectionName(currentPropertyBag.Id) is SectionName.ProjectDependencies)
                {
                    delayLoadProperties.Add((currentProject, currentPropertyBag));
                }
                else
                {
                    currentProject.AddSlnProperties(currentPropertyBag);
                }
            }
        }

        private static Encoding GetSlnFileEncoding(StreamReader reader)
        {
            // UTF-16 is supported, so roundtrip as is.
            if (reader.CurrentEncoding.CodePage == Encoding.Unicode.CodePage)
            {
                return Encoding.Unicode;
            }

            // If the file is UTF-8 and has a BOM then it should stay UTF-8.
            if (reader.CurrentEncoding.CodePage == Encoding.UTF8.CodePage &&
                !reader.CurrentEncoding.GetPreamble().IsNullOrEmpty())
            {
                return Encoding.UTF8;
            }

            // All other encodings default to ASCII. If it was a file with an ANSI codepage
            // encoding it will get converted to UTF-8 with BOM on save.
            // ASCII is subset of UTF-8, and it doesn't emit a BOM, and is compatible with old versions
            // of Visual Studio, so it is the preferred default for .sln files.
            return Encoding.ASCII;
        }

        private static string? CommentToOpenWithVS(StringSpan firstComment)
        {
            firstComment = firstComment.Trim();
            return
                firstComment.IsEmpty ? null :
                firstComment.StartsWith(SlnConstants.OpenWithPrefix) ? firstComment.Slice(SlnConstants.OpenWithPrefix.Length).ToString() :
                null;
        }

        // determine the line time and advance the scan position.
        private static LineType GetLineType(ref StringTokenizer tokenizer, bool allowSolutionProperties)
        {
            // skip all leading whitespace, note that is a relaxation from original file for some elements (like section will not require exactly 2 tabs - can be spaces).
            tokenizer.TrimStart();
            if (tokenizer.IsEmpty)
            {
                return LineType.Empty;
            }

            int first = tokenizer.CurrentPos; // used enforce begining of line in cases we dont want to allow leading spaces
            switch (tokenizer.CurrentChar)
            {
                case '#':
                    // extension to original recoginzing the # at any location, but only if is followed by space.
                    // original parser will ignore this line, but we want to preserve it as a whitespace. (on write we always fix it and move # to be first character)
                    if (first == 0)
                    {
                        return LineType.CommentLine;
                    }
                    else if (tokenizer[1].IsWhiteSpace())
                    {
                        return LineType.CommentLineEx;
                    }

                    break;

                case 'P':
                    // this can match either Project( and ProjectSection(
                    if (first == 0 && tokenizer.SliceIfStartsWith(SlnConstants.TagProjectStart))
                    {
                        return LineType.Project;
                    }

                    if (tokenizer.SliceIfStartsWith(SlnConstants.TagProjectSectionStart))
                    {
                        return LineType.ProjectSection;
                    }

                    break;

                case 'G':
                    // Global or GlobalSection(
                    // "Global" needs to start at 0, character after it be either whitespace, or be at the end of line.
                    if (first == 0 && tokenizer.SliceIfStartsWithAndEmptyAfter(SlnConstants.TagGlobal))
                    {
                        return LineType.Global;
                    }

                    if (tokenizer.SliceIfStartsWith(SlnConstants.TagGlobalSectionStart))
                    {
                        return LineType.GlobalSection;
                    }

                    break;

                case 'E':
                    // EndProject, EndGlobal , EndProjectSection and EndGlobalSection
                    if (first == 0)
                    {
                        if (tokenizer.SliceIfStartsWithAndEmptyAfter(SlnConstants.TagEndProject))
                        {
                            return LineType.EndProject;
                        }

                        if (tokenizer.SliceIfStartsWithAndEmptyAfter(SlnConstants.TagEndGlobal))
                        {
                            return LineType.EndGlobal;
                        }
                    }

                    if (tokenizer.SliceIfStartsWithAndEmptyAfter(SlnConstants.TagEndProjectSection))
                    {
                        return LineType.EndProjectSection;
                    }

                    if (tokenizer.SliceIfStartsWithAndEmptyAfter(SlnConstants.TagEndGlobalSection))
                    {
                        return LineType.EndGlobalSection;
                    }

                    break;

                case 'V':
                    // VisualStudioVersion
                    if (allowSolutionProperties && first == 0 && tokenizer.SliceIfStartsWith(SlnConstants.TagVisualStudioVersion))
                    {
                        return LineType.VisualStudioVersion;
                    }

                    break;

                case 'M':
                    // MinimumVisualStudioVersion
                    if (allowSolutionProperties && first == 0 && tokenizer.SliceIfStartsWith(SlnConstants.TagMinimumVisualStudioVersion))
                    {
                        return LineType.MinimumVisualStudioVersion;
                    }

                    break;
            }

            return LineType.Property;
        }

        // parsers propery "scope" value. aka preSolution, postSolution or preProject, postProject
        private static bool TryParseScope(scoped StringSpan s, bool isSolution, out PropertiesScope scope)
        {
            scope = PropertiesScope.PreLoad;
            if (s.IsEmpty)
            {
                return false;
            }

            if (s.EqualsOrdinal(isSolution ? SlnConstants.TagPreSolution : SlnConstants.TagPreProject))
            {
                scope = PropertiesScope.PreLoad;
                return true;
            }
            else if (s.EqualsOrdinal(isSolution ? SlnConstants.TagPostSolution : SlnConstants.TagPostProject))
            {
                scope = PropertiesScope.PostLoad;
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool TryParseFormatLine()
        {
            if (!this.ReadLine(out StringTokenizer tokenizer))
            {
                return false;
            }

            // skips first line if empty. (happen if UTF8 bom is used by writer)
            if (tokenizer.IsEmpty)
            {
                if (!this.ReadLine(out tokenizer))
                {
                    return false;
                }
            }

            if (tokenizer.Current.IndexOf(SlnConstants.SLNFileHeaderNoVersion) < 0)
            {
                // first line may contain file format signature, sp parsers will try the second line as well.
                if (!this.ReadLine(out tokenizer) || tokenizer.Current.IndexOf(SlnConstants.SLNFileHeaderNoVersion) < 0)
                {
                    return false;
                }
            }

            StringSpan versionPath = tokenizer.Current.SliceToLast(' ');
            if (!versionPath.IsEmpty)
            {
                versionPath = versionPath.Slice(1);
            }

            if (versionPath.IsEmpty)
            {
                return false;
            }

            int dotIndex = versionPath.IndexOf('.');
            string fileVersionMaj;
            if (dotIndex < 0)
            {
                fileVersionMaj = versionPath.ToString();

                // To dot or not to dot. That is an important question ...
                // return false;
            }
            else
            {
                // the old parser does not bother for .XX to be a integer, just not to be empty (spaces are ok)
                if (dotIndex + 1 >= versionPath.Length)
                {
                    return false;
                }

                fileVersionMaj = versionPath.Slice(0, dotIndex).ToString();
            }

            return !string.IsNullOrEmpty(fileVersionMaj) && int.TryParse(fileVersionMaj, out int fileVer) && fileVer <= 14;
        }

        private bool ReadLine(out StringTokenizer lineScanner)
        {
            string? line = reader.ReadLine();
            this.lineNumber++;
            lineScanner = new(line ?? string.Empty);
            return line is not null;
        }

        // Creates PropertyMap object from [Project|Global]Section( /// <sectionName>) = scope
        private SolutionPropertyBag? ReadPropertyBag(ref StringTokenizer tokenizer, bool isSolution, bool checkOnly)
        {
            // Not sure if it was a recent bug or always like thatthe old parser is kind of awkward it will allow any of these:
            // ...Section({any space,tab,(,),=}<Name>[any tab,(,)=]{any space,<tab>,=}<scope>{any space,tab,(,),=}{.*}
            // So that is valid: ProjectSection ((( ))XXX===(())preProect
            // We have to keep that behaviour, only slight difference  will allow space in adition to tab at the end of name
            // With all wierd syntaxes old will accepet, it will not accept ProjectSection( Foo )  (but will do ) ProjectSection(  Foo) ...
            StringSpan sectionName = tokenizer.NextToken(SlnConstants.SectionSeparators).Trim();
            this.ValidateAbort(!sectionName.IsEmpty);
            StringSpan sectionScopeStr = tokenizer.NextToken(SlnConstants.SectionSeparators).Trim();
            this.ValidateAbort(TryParseScope(sectionScopeStr, isSolution, out PropertiesScope scope));
            return checkOnly ? null : new SolutionPropertyBag(sectionName.ToString(), scope);
        }

        private SolutionItemModel ReadProjectInfo(SolutionModel solution, ref StringTokenizer tokenizer)
        {
            // Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "App1", "App1\App1.csproj", "{B0D4AB54-EB86-4C88-A2A4-C55D0C200244}"
            //         ^  <- this is tokenizer pos.
            // yes it is errata, these can be preceded with arbitrary number of ()=,space and quotes...
            StringSpan projectType = tokenizer.NextToken(SlnConstants.ProjectSeparators);

            // but it must end with [sep]) ... checked later.
            this.ValidateAbort(!projectType.IsEmpty);

            this.ValidateAbort(Guid.TryParse(projectType.ToString(), out Guid projectTypeId));

            // this just skips up to Display's name "App1" first quote, position at 'A". The TrimStart is extension to allow spaces before ')';
            // and yes, any characters are allowed for example // Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "App1", valid bad format :P "App1\App1.csproj",
            StringSpan skip = tokenizer.NextToken(SlnConstants.DoubleQuote).TrimStart();

            // and do the check for factory guid. ends with ').
            this.ValidateAbort(!skip.IsEmpty && skip[0] == ')');

            StringSpan displayName = tokenizer.NextToken(SlnConstants.DoubleQuote);
            this.ValidateAbort(!displayName.IsEmpty);
            skip = tokenizer.NextToken(SlnConstants.DoubleQuote).TrimStart();
            this.ValidateAbort(!skip.IsEmpty && skip[0] == ',');
            StringSpan relativePath = tokenizer.NextToken(SlnConstants.DoubleQuote);
            this.ValidateAbort(!relativePath.IsEmpty);

            // no comma check errata for this so any text between "relPath"{*}"uniqueiId" is valid.
            StringSpan projectUniqueId = tokenizer.NextToken(SlnConstants.ProjectSeparators);
            this.ValidateAbort(!projectUniqueId.IsEmpty);
            _ = this.Validate(Guid.TryParse(projectUniqueId.ToString(), out Guid projectId));

            if (projectTypeId == ProjectTypeTable.SolutionFolder)
            {
                SolutionFolderModel folder = solution.AddFolder(name: displayName.ToString());
                folder.Id = projectId;
                return folder;
            }
            else
            {
                SolutionProjectModel project = solution.AddProject(
                    filePath: PathExtensions.ConvertFromPersistencePath(relativePath.ToString()),
                    projectTypeId: projectTypeId);
                project.Id = projectId;
                project.DisplayName = displayName.ToString();
                return project;
            }
        }

        private readonly void OnParseError(ParseError code, string? message = null)
        {
            int line = this.lineNumber;

            throw new InvalidSolutionFormatException($"Error in {fullPath}: {code} {message ?? string.Empty} {(line != 0 ? $" at line : {line}" : string.Empty)}");
        }

        // Validate condition, that would mark solution file as "corrupted" if false
        // In these scenarios old parser would ignore the line (potentially throw aways some data) and move on.
        // CONSIDER: Update this to log the error and location.
        private bool Validate(bool condition)
        {
            if (!condition)
            {
                this.corrupted = true;
            }

            return condition;
        }

        private bool Validate<T>([NotNullWhen(true)] T? obj)
            where T : class
        {
            return this.Validate(obj is not null) && obj is not null;
        }

        // Validate condition, that if false would make so the old parser will give up and report failure and reject the solution file.
        private void ValidateAbort(bool condition)
        {
            if (condition)
            {
                return;
            }

            if (this.Validate(condition))
            {
                return;
            }

            this.OnParseError(ParseError.BadSln12File, "Bad solution file");
        }
    }
}
