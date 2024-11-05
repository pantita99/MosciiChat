using System;

namespace Server.Components
{
    public class AppConfig
    {
        public string Host { get; set; } = string.Empty;
        public string Port { get; set; } = string.Empty;
        public string IPAddress { get; set; } = string.Empty;
        public string Allow { get; set; } = string.Empty;
        public string webPagesVersion { get; set; } = string.Empty;
        public bool webPagesEnabled { get; set; } = false;
        public bool PreserveLoginUrl { get; set; } = true;
        public bool ClientValidationEnabled { get; set; } = true;
        public bool UnobtrusiveJavaScriptEnabled { get; set; } = true;
        public string StarCatWCFPath { get; set; } = string.Empty;
        public string BaseServerCode { get; set; } = string.Empty;
        public string UploadPath { get; set; } = string.Empty;
        public string LocalUploadPath { get; set; } = string.Empty;
        public int MaxDownloader { get; set; } = 2;
        public bool EnableFileTransferQueue { get; set; } = true;
        public bool EnableSaveInventoryFile { get; set; } = true;
        public string InventoryFolderPath { get; set; } = string.Empty;
        public string WebConsolePath { get; set; } = string.Empty;
        public string KeySecurity { get; set; } = string.Empty;
        public string ProductEdition { get; set; } = string.Empty;
        public int LicenseClient { get; set; } = 50000;
        public string Site { get; set; } = string.Empty;
        public string PTTDownloadFolderURL { get; set; } = string.Empty;
        public string PTTUploadFolder { get; set; } = string.Empty;
        public string PTTDownloadFolder { get; set; } = string.Empty;
        public string SaveTempComputersMongoDB { get; set; } = string.Empty;
        public string AnnomnentHostLoadFile { get; set; } = string.Empty;
        public bool EnableAnnomnent { get; set; } = true;
        public bool EnableUSB { get; set; } = true;
        public bool EnableTaskCommand { get; set; } = false;
        public bool EnableTaskFile { get; set; } = true;
        public bool EnableDisallow { get; set; } = true;
        public bool OnlyAsset { get; set; } = false;
    }
}
