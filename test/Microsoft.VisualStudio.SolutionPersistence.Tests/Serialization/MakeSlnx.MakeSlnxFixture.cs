// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Serialization;

#pragma warning disable CS9113 // Parameter is unread.
public partial class MakeSlnx
#pragma warning restore CS9113 // Parameter is unread.
{
    public class MakeSlnxFixture
    {
        public MakeSlnxFixture()
        {
            ClearDirectory();
        }

        public static void ClearDirectory()
        {
            string outputDirectory = Path.Combine(Path.GetTempPath(), "OutputSln");
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, true);
            }

            _ = Directory.CreateDirectory(outputDirectory);

            string convertedSlnx = Path.Join(outputDirectory, "slnx");
            string sln = Path.Join(outputDirectory, "sln");
            string slnThruSlnxStream = Path.Join(outputDirectory, "slnThruSlnxStream");
            _ = Directory.CreateDirectory(convertedSlnx);
            _ = Directory.CreateDirectory(sln);
            _ = Directory.CreateDirectory(slnThruSlnxStream);
        }
    }
}
