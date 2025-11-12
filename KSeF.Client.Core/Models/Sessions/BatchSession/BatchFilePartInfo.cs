namespace KSeF.Client.Core.Models.Sessions.BatchSession
{

    public class BatchFilePartInfo
    {
        public int OrdinalNumber { get; set; }

        [System.Obsolete("FileName jest przestarzały i wkrótce zostanie usunięty.", error: false)]
        public string FileName { get; set; }

        public long FileSize { get; set; }

        public string FileHash { get; set; }

    }

}
