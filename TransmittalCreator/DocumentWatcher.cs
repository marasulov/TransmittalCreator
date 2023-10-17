using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Timers;

namespace TransmittalCreator;

public class DocumentWatcher : IDisposable
{
	public delegate void DocumentHandler(string message);

	public delegate void TimeHandler();

	private readonly string[] _fileNames;


	private string _targetDictionary;

	private double _waitingInterval;

	private Timer timer;
	private FileSystemWatcher watcher;

	public DocumentWatcher(string[] fileNames, string targetDirectory, double waitingInterval)
	{
		_fileNames = fileNames;
		TargetDirectory = targetDirectory;
		_waitingInterval = waitingInterval;
	}

	public int Count { get; set; }

	public double WaitingInterval
	{
		get => _waitingInterval;
		set
		{
			if (value < 0) throw new ArgumentException("время задано не правильно");

			_waitingInterval = value;
		}
	}

	public string TargetDirectory
	{
		get => _targetDictionary;
		set
		{
			if (string.IsNullOrEmpty(value) & !Directory.Exists(value))
				throw new ArgumentException("путь задан не правильно");
			_targetDictionary = value;
		}
	}

	public void Dispose()
	{
		timer?.Dispose();
		watcher?.Dispose();
	}

	public event DocumentHandler DocumentReady;
	public event TimeHandler TimeOut;

	public void Start()
	{
		watcher = new FileSystemWatcher(TargetDirectory)
		{
			NotifyFilter = NotifyFilters.CreationTime
			               | NotifyFilters.FileName
		};

		watcher.Created += OnCreated;
		watcher.Renamed += OnRenamed;
		watcher.Filter = "*.*";

		watcher.IncludeSubdirectories = true;
		watcher.EnableRaisingEvents = true;

		timer = new Timer(WaitingInterval);
		timer.Start();
		timer.Elapsed += OnTimeElapsed;
	}

	private void OnTimeElapsed(object sender, ElapsedEventArgs e)
	{
		timer.Stop();
		watcher.Created -= OnCreated;
		watcher.Renamed -= OnRenamed;
		timer.Elapsed -= OnTimeElapsed;

		if (Count != _fileNames.Length)
		{
			TimeOut?.Invoke();
		}
		else
		{
			DocumentReady?.Invoke("Врямя закончилось, вы успели загрузить файлы");
			timer?.Stop();
		}
	}

	private void OnRenamed(object sender, RenamedEventArgs e)
	{
		var fileName = Path.GetFileName(e.FullPath);

		if (Regex.IsMatch(fileName, "Паспорт.jpg|Заявление.txt|Фото.jpg", RegexOptions.IgnoreCase))
			Console.WriteLine($"файл изменен в {fileName}. Загрузите файл");
	}

	private void OnCreated(object sender, FileSystemEventArgs e)
	{
		var fileName = Path.GetFileName(e.FullPath);

		if (Regex.IsMatch(fileName, "Паспорт.jpg|Заявление.txt|Фото.jpg", RegexOptions.IgnoreCase))
		{
			Console.WriteLine($"файл {fileName} загружен");
			Count++;
			if (Count == _fileNames.Length)
			{
				DocumentReady?.Invoke("файлы успешно загружены");
				timer.Stop();
			}
		}
	}
}