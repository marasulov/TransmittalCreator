namespace TransmittalCreator.Services.Files
{
    public interface IFileManager
    {
        string FileName { get;  }
        string DirPath { get; }

        string GetFilePath();
    }
}