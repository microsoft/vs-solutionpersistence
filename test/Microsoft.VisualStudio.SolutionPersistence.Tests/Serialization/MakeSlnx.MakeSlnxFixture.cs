// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Serialization;

public sealed partial class MakeSlnx
{
    public const string OutputDirectory = "OutputSln";

    /// <summary>
    /// Used by <see cref="MakeSlnx"/> tests to ensure temp directory structure is created and
    /// empty before the tests are run.
    /// </summary>
    public class MakeSlnxFixture
    {
        public MakeSlnxFixture()
        {
            string outputDirectory = Path.Combine(Path.GetTempPath(), OutputDirectory);
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, true);
            }

            _ = Directory.CreateDirectory(outputDirectory);

            this.SlnToSlnxDirectory = Path.Join(outputDirectory, "slnToSlnx");
            this.SlnViaSlnxDirectory = Path.Join(outputDirectory, "slnViaSlnx");
            this.SlnxToSlnDirectory = Path.Join(outputDirectory, "slnxToSln");
            _ = Directory.CreateDirectory(this.SlnToSlnxDirectory);
            _ = Directory.CreateDirectory(this.SlnViaSlnxDirectory);
            _ = Directory.CreateDirectory(this.SlnxToSlnDirectory);
        }

        public string SlnToSlnxDirectory { get; private set; }

        public string SlnViaSlnxDirectory { get; private set; }

        public string SlnxToSlnDirectory { get; private set; }
    }
}
