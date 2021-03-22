# Party Playlist Game

 If you want to see Database Design you can click [here](DB_Schema.png). 

 I used postgreSQL as a database where all user-, library-, playlist- and battle data will be saved.
 My connection string is :

``   
private static readonly string connection =
"Server=localhost;Port=5435;Database=playlist;User Id=postgres;Password=postgres;";
``

Be sure to change the port, userid, passw and database (or you can create a database with se same name).  

For the database tables, I saved the code into sql files so you can easily excecute them. I saved them in a file, because I had to change the tables as I was developing this application. So it was easier for me to keep track of the changes. 
The files are [here](PlaylistGame/Datenbank/)
