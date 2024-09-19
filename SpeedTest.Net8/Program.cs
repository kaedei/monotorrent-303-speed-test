//Author: Kaedei
//Date: 2024-09-19
//Use monotorrent (3.0.3-alpha.unstable.rev0049) to load multiple torrent files and download them
//observe if the download speed drop to 0 issue happens

using MonoTorrent;
using MonoTorrent.Client;

var settingsBuilder = new EngineSettingsBuilder
{
    DiskCacheBytes = 16 * 1024 * 1024, //16MB
    MaximumHalfOpenConnections = 256,
};

//init monotorrent engine
using var engine = new ClientEngine(settingsBuilder.ToSettings());
var torrentManagerList = new List<TorrentManager>();

//add *.torrent files under /torrents folder to engine
var torrents = Directory.GetFiles("torrents", "*.torrent").Select(Torrent.Load);
var customTrackers = File.ReadAllLines("trackers.txt")
    .Where(l => l.StartsWith("http") || l.StartsWith("tcp") || l.StartsWith("udp"))
    .ToList();
foreach (var torrent in torrents)
{
    Console.WriteLine("Adding torrent: " + torrent.Name);
    var torrentManager = await engine.AddAsync(torrent, "downloads");
    //add more trackers
    foreach (var customTracker in customTrackers)
    {
        await torrentManager.TrackerManager.AddTrackerAsync(new Uri(customTracker));
    }

    torrentManagerList.Add(torrentManager);
    await torrentManager.StartAsync();
}

//refresh all task status every 1 second
var startTime = DateTime.Now;
while (true)
{
    Console.Clear();
    Console.WriteLine("Time elapsed: " + (DateTime.Now - startTime));
    Console.WriteLine("Status | Progress | Download Speed | Upload Speed | Name");
    foreach (var torrentManager in torrentManagerList)
    {
        Console.WriteLine(
            $"{torrentManager.State} | {torrentManager.Progress:0.0}% | {torrentManager.Monitor.DownloadRate / 1024} KB/s | {torrentManager.Monitor.UploadRate / 1024} KB/s | {torrentManager.Torrent?.Name}");
    }

    await Task.Delay(1000);
}