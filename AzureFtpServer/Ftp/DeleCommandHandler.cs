using System.Text;
using System.Diagnostics;
using System.Configuration;
using AzureFtpServer.Ftp;
using AzureFtpServer.Provider;


namespace AzureFtpServer.FtpCommands
{
    /// <summary>
    /// DELE command handler
    /// delete a file
    /// </summary>
    internal class DeleCommandHandler : FtpCommandHandler
    {
        public DeleCommandHandler(FtpConnectionObject connectionObject)
            : base("DELE", connectionObject)
        {
        }

        protected override string OnProcess(string sMessage)
        {
            sMessage = sMessage.Trim();
            if (sMessage == "")
                return GetMessage(501, $"{Command} needs a parameter");

            string fileToDelete = GetPath(sMessage);
            Trace.TraceInformation($"DELE {fileToDelete} - BEGIN");
            Stopwatch sw = new Stopwatch();
            sw.Start();
            
            // 2015-11-24 cljung : Q&D fix. If path contains double slashes, reduce to single since
            //                     NTFS/etc treams sub1//sub2 as sub1/sub two but Azure Blob Storage doesn't
            if (ConnectionObject.FileSystemObject.FileExists(fileToDelete) )
            {
                if (!StorageProviderConfiguration.FtpReplaceSlashOnDELE)
                    fileToDelete = fileToDelete.Replace("//", "/");
                else
                {
                    FtpServer.LogWrite(this, sMessage, 550, sw.ElapsedMilliseconds);
                    return GetMessage(550, $"File \"{fileToDelete}\" does not exist.");
                }
            }

            if (!ConnectionObject.FileSystemObject.FileExists(fileToDelete))
            {
                FtpServer.LogWrite(this, sMessage, 550, sw.ElapsedMilliseconds);
                return GetMessage(550, $"File \"{fileToDelete}\" does not exist.");
            }

            if (!ConnectionObject.FileSystemObject.DeleteFile(fileToDelete))
            {
                FtpServer.LogWrite(this, sMessage, 550, sw.ElapsedMilliseconds);
                return GetMessage(550, $"Delete file \"{fileToDelete}\" failed.");
            }
            sw.Stop();
            Trace.TraceInformation($"DELE {fileToDelete} - END, Time {sw.ElapsedMilliseconds} ms");

            FtpServer.LogWrite(this, sMessage, 250, sw.ElapsedMilliseconds);
            return GetMessage(250, $"{Command} successful. Time {sw.ElapsedMilliseconds} ms");
        }
    }
}