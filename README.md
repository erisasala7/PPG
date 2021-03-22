# Party Playlist Game

 If you want to see Database Design you can click [here](DB_Schema.png). 

 I used postgreSQL as a database where all user-, library-, playlist- and battle data will be saved.
 My connection string is :

``   
private static readonly string connection =
"Server=localhost;Port=5435;Database=playlist;User Id=postgres;Password=postgres;";
``

Be sure to change the port, userid, passw and database (or you can create a database with the same name).  

For the database tables, I saved the code into sql files so you can easily excecute them. I saved them in a file, because I had to change the tables as I was developing this application. So it was easier for me to keep track of the changes. 
The files are [here](PlaylistGame/Datenbank/)


I devided my program in two parts: REST and Game. 

The [REST](PlaylistGame/REST/Interfaces/) Folder ist the server part. There are the needed Classes and Interfaces for a http Server. 
The [Game](PlaylistGame/Game/) Folder contains a file with all the nessasary methods for the game. The file can be found [here](PlaylistGame/Game/Game.cs)

Another part of the assigment was that we had to do at least 10 unit tests. In [here]() is the test File. I did testmethods for the methods that I made in the [Game]() File. 
I decided to so these test because in my opinion, these methods are important and need to be right.

Hours : 80-90

[PartyPlaylistBattleGame Github](https://github.com/erisasala7/PPG.git) (Master Branch)
