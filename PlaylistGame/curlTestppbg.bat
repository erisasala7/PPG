@echo off

REM --------------------------------------------------
REM Party Playlist Battle
REM --------------------------------------------------
title Party Playlist Battle
echo CURL Testing for Party Playlist Battle
echo.

REM --------------------------------------------------
echo 1) Create Users (Registration)
REM Create User
curl -X POST http://localhost:10002/users --header "Content-Type: application/json" -d "{\"Username\":\"kienboec\", \"Password\":\"daniel\"}"
echo.
curl -X POST http://localhost:10002/users --header "Content-Type: application/json" -d "{\"Username\":\"altenhof\", \"Password\":\"markus\"}"
echo.
curl -X POST http://localhost:10002/users --header "Content-Type: application/json" -d "{\"Username\":\"admin\",    \"Password\":\"istrator\"}"
echo.

echo should fail:
curl -X POST http://localhost:10002/users --header "Content-Type: application/json" -d "{\"Username\":\"kienboec\", \"Password\":\"daniel\"}"
echo.
curl -X POST http://localhost:10002/users --header "Content-Type: application/json" -d "{\"Username\":\"kienboec\", \"Password\":\"different\"}"
echo. 
echo.

REM --------------------------------------------------
echo 2) Login Users
curl -X POST http://localhost:10002/sessions --header "Content-Type: application/json" -d "{\"Username\":\"kienboec\", \"Password\":\"daniel\"}"
echo.
curl -X POST http://localhost:10001/sessions --header "Content-Type: application/json" -d "{\"Username\":\"altenhof\", \"Password\":\"markus\"}"
echo.
curl -X POST http://localhost:10001/sessions --header "Content-Type: application/json" -d "{\"Username\":\"admin\",    \"Password\":\"istrator\"}"
echo.

echo should fail:
curl -X POST http://localhost:10002/sessions --header "Content-Type: application/json" -d "{\"Username\":\"kienboec\", \"Password\":\"different\"}"
echo.
echo.
REM --------------------------------------------------
echo 3) edit user data
echo.
curl -X GET http://localhost:10002/users/kienboec --header "Authorization: Basic kienboec-ppbToken"
echo.
curl -X GET http://localhost:10001/users/altenhof --header "Authorization: Basic altenhof-ppbToken"
echo should fail:
curl -X GET http://localhost:10002/users/altenhof --header "Authorization: Basic kienboec-ppbToken"
echo.
curl -X GET http://localhost:10001/users/kienboec --header "Authorization: Basic altenhof-ppbToken"
curl -X GET http://localhost:10001/users/someGuy  --header "Authorization: Basic kienboec-ppbToken"

REM --------------------------------------------------
echo 6) get library
curl -X GET http://localhost:10001/lib --header "Authorization: Basic kienboec-ppbToken"
curl -X GET http://localhost:10001/lib --header "Authorization: Basic altenhof-ppbToken"
echo.
echo.

REM --------------------------------------------------
echo 7) manipulate (add)
curl -X POST http://localhost:10002/lib --header "Content-Type: application/json" --header "Authorization: Basic altenhof-ppbToken" -d "{\"Name\": \"Best_song_ever5\", \"Url\": \"https://www.youtube.com/watch?v=dQw4w9WgXcQ\", \"Rating\": 5, \"Genre\": \"Pop\"}"
curl -X POST http://localhost:10002/lib --header "Content-Type: application/json" --header "Authorization: Basic kienboec-ppbToken" -d "{\"Name\": \"Good_Mood_Song\", \"Url\": \"https://youtu.be/H9cmPE88a_0\", \"Rating\": 4, \"Genre\": \"Pop\", \"Title\": \"Duck Tales Intro\", \"Length\": \"2:52\", \"Album\": \"Theme Songs\"}"
curl -X POST http://localhost:10001/lib --header "Content-Type: application/json" --header "Authorization: Basic altenhof-ppbToken" -d "{\"Name\": \"Super_Mario_song\", \"Url\": \"https://www.youtube.com/watch?v=wyoNnMO3zFk\", \"Rating\": 4, \"Genre\": \"Game Music\", \"Title\": \"Super Mario\"}"
echo.
echo.
REM --------------------------------------------------
echo 9) manipulate (remove)
curl -X DELETE http://localhost:10002/lib/Best_song_ever5 --header "Authorization: Basic altenhof-ppbToken"

echo should fail:
curl -X DELETE http://localhost:2/lib/Best_song_ever --header "Authorization: Basic altenhof-ppbToken"
curl -X DELETE http://localhost:10001/lib/unknown --header "Authorization: Basic altenhof-ppbToken"
curl -X DELETE http://localhost:10001/lib/Super_Mario_song --header "Authorization: Basic kienboec-ppbToken"
echo.
echo.
echo 11) add song to playlist
curl -X POST http://localhost:10002/playlist --header "Content-Type: application/json" --header "Authorization: Basic altenhof-ppbToken" -d "{\"Name\": \"Super_Mario_song\"}"
curl -X POST http://localhost:10002/playlist --header "Content-Type: application/json" --header "Authorization: Basic altenhof-ppbToken" -d "{\"Name\": \"Super_Mario_song\"}"
curl -X POST http://localhost:10002/playlist --header "Content-Type: application/json" --header "Authorization: Basic altenhof-ppbToken" -d "{\"Name\": \"Super_Mario_song\"}"

curl -X POST http://localhost:10002/playlist --header "Content-Type: application/json" --header "Authorization: Basic kienboec-ppbToken" -d "{\"Name\": \"Good_Mood_Song\"}"
curl -X POST http://localhost:10002/playlist --header "Content-Type: application/json" --header "Authorization: Basic kienboec-ppbToken" -d "{\"Name\": \"Good_Mood_Song\"}"
echo.
echo.
REM --------------------------------------------------
echo 10) get playlist
curl -X GET http://localhost:10002/playlist
echo.
echo.
