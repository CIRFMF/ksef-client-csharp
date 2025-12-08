using System;
using System.IO;
using System.Linq;

namespace KSeF.Client.Tests.CertTestApp
{
    public static class UploadedInvoiceFiles
    {
        public static string[] GetUploadedInvoiceFiles(string directory)
        {
            if (!Directory.Exists(directory))
                return Array.Empty<string>();
            return Directory.GetFiles(directory, "*.xml");
        }
    }
}
