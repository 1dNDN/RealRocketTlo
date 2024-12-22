// See https://aka.ms/new-console-template for more information

using NativeTloLibrary;
using NativeTloLibrary.Database;
using NativeTloLibrary.StaticApi;

Console.WriteLine("Hello, World!");

var keeperNames = await KeepersList.GetKeepersAsync();

var subsectionsYoba = new SubsectionsYoba();

var (topics, keepers) = await subsectionsYoba.Read(keeperNames);

var db = new DatabaseLowLvl("C:\\\\Games\\\\webtlo-win-2.6.0-beta1\\\\webtlo-win\\\\nginx\\\\wtlo\\\\data\\\\webtlofordotnet.db");

db.LoadTopicsByChunksWithParameter(topics.ToArray());
db.LoadSeedersByChunksWithParameter(keepers.ToArray());
