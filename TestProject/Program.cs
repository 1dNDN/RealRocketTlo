// See https://aka.ms/new-console-template for more information

using NativeTloLibrary;

using TloSql;

Console.WriteLine("Hello, World!");

var subsectionsYoba = new SubsectionsYoba();

var (topics, keepers) = await subsectionsYoba.Read();

