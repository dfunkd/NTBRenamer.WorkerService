using System.Runtime.InteropServices;

namespace NTBRenamer.WorkerService.Core;

public class FileExtension
{
    [DllImport(@"urlmon.dll", CharSet = CharSet.Auto)]
    private extern static UInt32 FindMimeFromData(UInt32 pBC, [MarshalAs(UnmanagedType.LPWStr)] String pwzUrl, [MarshalAs(UnmanagedType.LPArray)] byte[] pBuffer, UInt32 cbSize,
        [MarshalAs(UnmanagedType.LPWStr)] String pwzMimeProposed, UInt32 dwMimeFlags, out UInt32 ppwzMimeOut, UInt32 dwReserverd);

    private static readonly byte[] BMP = { 66, 77 };
    private static readonly byte[] DOC = { 208, 207, 17, 224, 161, 177, 26, 225 };
    private static readonly byte[] EXE_DLL = { 77, 90 };
    private static readonly byte[] GIF = { 71, 73, 70, 56 };
    private static readonly byte[] ICO = { 0, 0, 1, 0 };
    private static readonly byte[] JPG = { 255, 216, 255 };
    private static readonly byte[] MP3 = { 255, 251, 48 };
    private static readonly byte[] OGG = { 79, 103, 103, 83, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0 };
    private static readonly byte[] PDF = { 37, 80, 68, 70, 45, 49, 46 };
    private static readonly byte[] PNG = { 137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 13, 73, 72, 68, 82 };
    private static readonly byte[] RAR = { 82, 97, 114, 33, 26, 7, 0 };
    private static readonly byte[] SWF = { 70, 87, 83 };
    private static readonly byte[] TIFF = { 73, 73, 42, 0 };
    private static readonly byte[] TORRENT = { 100, 56, 58, 97, 110, 110, 111, 117, 110, 99, 101 };
    private static readonly byte[] TTF = { 0, 1, 0, 0, 0 };
    private static readonly byte[] WAV_AVI = { 82, 73, 70, 70 };
    private static readonly byte[] WMV_WMA = { 48, 38, 178, 117, 142, 102, 207, 17, 166, 217, 0, 170, 0, 98, 206, 108 };
    private static readonly byte[] ZIP_DOCX = { 80, 75, 3, 4 };

    public static string GetMimeType(byte[] file, string fileName)
    {
        string[] exclude = ["octet-stream", "x-msdownload", "oga", "ogg", "ogx"];
        string mime = "octet-stream"; //DEFAULT UNKNOWN MIME TYPE

        //Ensure that the filename isn't empty or null
        if (string.IsNullOrWhiteSpace(fileName))
            return mime;

        //Get the file extension
        string extension = Path.GetExtension(fileName) == null ? string.Empty : Path.GetExtension(fileName).ToUpper();

        //Get the MIME Type
        if (file.Take(2).SequenceEqual(BMP))
            mime = "bmp";
        else if (file.Take(8).SequenceEqual(DOC))
            mime = "docx";
        else if (file.Take(2).SequenceEqual(EXE_DLL))
            mime = "x-msdownload"; //both use same mime type
        else if (file.Take(4).SequenceEqual(GIF))
            mime = "gif";
        else if (file.Take(4).SequenceEqual(ICO))
            mime = "ico";
        else if (file.Take(3).SequenceEqual(JPG))
            mime = "jpg";
        else if (file.Take(3).SequenceEqual(MP3))
            mime = "mp3";
        else if (file.Take(14).SequenceEqual(OGG))
        {
            if (extension == ".OGX")
                mime = "ogg";
            else if (extension == ".OGA")
                mime = "ogg";
            else
                mime = "ogg";
        }
        else if (file.Take(7).SequenceEqual(PDF))
            mime = "pdf";
        else if (file.Take(16).SequenceEqual(PNG))
            mime = "png";
        else if (file.Take(7).SequenceEqual(RAR))
            mime = "rar";
        else if (file.Take(3).SequenceEqual(SWF))
            mime = "swf";
        else if (file.Take(4).SequenceEqual(TIFF))
            mime = "tiff";
        else if (file.Take(11).SequenceEqual(TORRENT))
            mime = "torrent";
        else if (file.Take(5).SequenceEqual(TTF))
            mime = "ttf";
        else if (file.Take(4).SequenceEqual(WAV_AVI))
            mime = "avi";
        else if (file.Take(16).SequenceEqual(WMV_WMA))
            mime = "wma";
        else if (file.Take(4).SequenceEqual(ZIP_DOCX))
            mime = "docx";

        return exclude.Contains(mime) ? string.Empty : mime;
    }
}
