using System;

namespace connsearch
{

    class PrefixedVersion
    {
        public string VersionString { get; private set; }
        public string Prefix { get; private set; }

        public int Major { get; private set; }
        public int Minor { get; private set; }
        public int MajorRevision { get; private set; }
        public int MinorRevision { get; private set; }
        public int Build { get; private set; }

        public PrefixedVersion(string version, string prefix)
        {
            string extractedVersionNumber = version.Replace(prefix, "");
            string[] versionSections = extractedVersionNumber.Split(".");

            this.VersionString = version;

            this.Major = Convert.ToInt32(versionSections[0]);
            this.Minor = Convert.ToInt32(versionSections[1]);
            this.MajorRevision = Convert.ToInt32(versionSections[2]);
            this.MinorRevision = Convert.ToInt32(versionSections[3]);
            this.Build = Convert.ToInt32(versionSections[4]);
        }
    }

}