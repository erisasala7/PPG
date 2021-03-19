create table player
(
    userid       serial,
    username     varchar(255) UNIQUE,
    password     varchar(255),
    nickname     varchar(255),
    email        varchar(255),
    bio          varchar(255),
    token        varchar(255),
    games_played int,
    points       int,
    image        varchar(255),
    actions      varchar(255),
    admin        int


)
;
create table library
(
    libid    serial,
    name     varchar(255),
    url      varchar(255),
    rating   varchar(255),
    genre    varchar(255),
    username varchar(255) references player (username)

)
;
create table playlist
(
    pid      serial,
    songname varchar(255),
    username varchar(255) references player (username),
    position int
)
;

create table stats
(
    statsid  serial,
    username varchar(255) references player (username),
    wins     int


)
;
create table scoreboard
(
    statsid  serial,
    username varchar(255) references player (username),
    wins     int


)
;
create table battle
(
    battleid      serial,
    username1     varchar(255),
    username2     varchar(255),
    timeofplay    timestamp,
    winner        varchar(255),
    actionOfUser1 varchar(255),
    actionOfUser2 varchar(255)

)
;