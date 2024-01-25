CREATE DATABASE MTCG_DB;


CREATE TABLE IF NOT EXISTS users (
                                     id              serial         PRIMARY KEY,
                                     username        VARCHAR(255)    NOT NULL UNIQUE,
                                     password        VARCHAR(255)    NOT NULL,
                                     name            VARCHAR(255),
                                     bio             TEXT,
                                     image           TEXT,
                                     Coins           INT     DEFAULT 20,
                                     elo             INTEGER DEFAULT 1000,
                                     wins            INTEGER DEFAULT 0,
                                     losses          INTEGER DEFAULT 0,
                                     games           INTEGER DEFAULT 0
);

CREATE TABLE Cards (
                       Id UUID PRIMARY KEY,
                       Name VARCHAR(255),
                       Damage FLOAT,
                       Element VARCHAR(255),
                       Class VARCHAR(255),
                       Type VARCHAR(255)

);


CREATE TABLE UserCards (
                           CardId UUID,
                           UserId serial,
                           InDeck BOOLEAN,
                           FOREIGN KEY (CardId) REFERENCES Cards(Id),
                           FOREIGN KEY (UserId) REFERENCES Users(Id)
);





CREATE TABLE Packages (
                          PackageId UUID PRIMARY KEY,
                        CreatedTimestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE CardPackage (
                             CardId UUID,
                             PackageId UUID,
                             FOREIGN KEY (CardId) REFERENCES Cards(Id),
                             FOREIGN KEY (PackageId) REFERENCES Packages(PackageId)
);


CREATE TABLE IF NOT EXISTS trading_deals (
                                             id              UUID            PRIMARY KEY,
                                             CardToTrade     UUID            NOT NULL,
                                             Type            VARCHAR(255)    NOT NULL,
                                             MinimumDamage   FLOAT           NOT NULL,
                                             UserId          serial          NOT NULL,
                                             FOREIGN KEY (CardToTrade) REFERENCES Cards(Id)

);


CREATE VIEW leaderboard AS
SELECT username, elo, wins, losses
FROM users
ORDER BY elo DESC, wins DESC;



--Delete everything

Drop table trading_deals;
DELETE FROM trading_deals;
DELETE FROM usercards;
DELETE FROM Packages;
Delete From Users;
DELETE FROM Cards;
DELETE FROM CardPackage;