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


-- Create a function to calculate the expected outcome
CREATE OR REPLACE FUNCTION calculate_expected_outcome(elo1 INT, elo2 INT) RETURNS FLOAT AS $$
BEGIN
    RETURN 1.0 / (1.0 + POWER(10.0, (elo2 - elo1) / 400.0));
END;
$$ LANGUAGE plpgsql;

-- Create a function to calculate the K factor based on the number of games played
CREATE OR REPLACE FUNCTION calculate_k_factor(games INT) RETURNS INT AS $$
BEGIN
    IF games < 30 THEN
        RETURN 32;
    ELSE
        RETURN 24;
    END IF;
END;
$$ LANGUAGE plpgsql;

-- Create a function to calculate the Elo rating after a game
CREATE OR REPLACE FUNCTION calculate_elo(elo1 INT, elo2 INT, result CHAR) RETURNS INT AS $$
DECLARE
    expected_outcome FLOAT;
    k_factor1 INT;
    k_factor2 INT;
    actual_outcome INT;
BEGIN
    expected_outcome := calculate_expected_outcome(elo1, elo2);
    k_factor1 := calculate_k_factor((SELECT games FROM users WHERE elo = elo1));
    k_factor2 := calculate_k_factor((SELECT games FROM users WHERE elo = elo2));

    IF result = 'win' THEN
        actual_outcome := 1;
    ELSIF result = 'loss' THEN
        actual_outcome := 0;
    ELSE
        actual_outcome := 0.5; -- For a draw
    END IF;

    RETURN ROUND(elo1 + k_factor1 * (actual_outcome - expected_outcome));
END;
$$ LANGUAGE plpgsql;

-- Create a function to update Elo ratings after a game
CREATE OR REPLACE FUNCTION update_elo_after_game(player1_id INT, player2_id INT, result CHAR) RETURNS VOID AS $$
BEGIN
    -- Calculate new Elo ratings for both players
    UPDATE users
    SET elo = calculate_elo(elo, (SELECT elo FROM users WHERE id = player2_id), result),
        wins = CASE WHEN result = 'win' THEN wins + 1 ELSE wins END,
        losses = CASE WHEN result = 'loss' THEN losses + 1 ELSE losses END,
        games = games + 1
    WHERE id = player1_id;

    UPDATE users
    SET elo = calculate_elo(elo, (SELECT elo FROM users WHERE id = player1_id), result),
        wins = CASE WHEN result = 'loss' THEN wins + 1 ELSE wins END,
        losses = CASE WHEN result = 'win' THEN losses + 1 ELSE losses END,
        games = games + 1
    WHERE id = player2_id;
END;
$$ LANGUAGE plpgsql;

CREATE TABLE Cards (
                       Id UUID PRIMARY KEY,
                       Name VARCHAR(255),
                       Damage FLOAT,
                       Element VARCHAR(255),
                       Class VARCHAR(255)

);

CREATE TABLE UserCards (
                           CardId UUID,
                           UserId serial,
                           InDeck BOOLEAN,
                           FOREIGN KEY (CardId) REFERENCES Cards(Id),
                           FOREIGN KEY (UserId) REFERENCES Users(Id)
);





CREATE TABLE Packages (
                          PackageId UUID PRIMARY KEY
);

CREATE TABLE CardPackage (
                             CardId UUID,
                             PackageId UUID,
                             FOREIGN KEY (CardId) REFERENCES Cards(Id),
                             FOREIGN KEY (PackageId) REFERENCES Packages(PackageId)
);





CREATE VIEW leaderboard AS
SELECT username, elo, wins, losses
FROM users
ORDER BY elo DESC, wins DESC;



--Delete everything


DELETE FROM usercards;
DELETE FROM Packages;
Delete From Users;
DELETE FROM Cards;
DELETE FROM CardPackage;
