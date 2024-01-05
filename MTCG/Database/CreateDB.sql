CREATE DATABASE MTCG_DB;


CREATE TABLE IF NOT EXISTS users (
    id              serial          PRIMARY KEY,
    username        VARCHAR(255)    NOT NULL UNIQUE,
    password        VARCHAR(255)    NOT NULL,
    name            VARCHAR(255),
    bio             TEXT,
    image           TEXT,
    coins           integer default 20,
    elo             INTEGER DEFAULT 1000,
    wins            INTEGER DEFAULT 0,
    losses          INTEGER DEFAULT 0,
    IsAdmin         BOOLEAN DEFAULT FALSE
    
    );

CREATE TABLE IF NOT EXISTS cards (
                                     id              UUID            PRIMARY KEY,
                                     name            VARCHAR(255)    NOT NULL,
    damage          FLOAT           NOT NULL
    -- Add more attributes here as needed
    );

CREATE TABLE IF NOT EXISTS user_cards (
                                          user_id         INTEGER         NOT NULL,
                                          card_id         UUID            NOT NULL,
                                          PRIMARY KEY (user_id, card_id),
    FOREIGN KEY (user_id) REFERENCES users(id),
    FOREIGN KEY (card_id) REFERENCES cards(id)
    );

CREATE TABLE IF NOT EXISTS trading_deals (
                                             id              UUID            PRIMARY KEY,
                                             user_id         INTEGER         NOT NULL,
                                             card_to_trade   UUID            NOT NULL,
                                             type            VARCHAR(255)    NOT NULL,
    minimum_damage  FLOAT           NOT NULL,
    FOREIGN KEY (user_id) REFERENCES users(id),
    FOREIGN KEY (card_to_trade) REFERENCES cards(id)
    );

CREATE TABLE IF NOT EXISTS matches (
                                       id              serial          PRIMARY KEY,
                                       player_one_id   INTEGER         NOT NULL,
                                       player_two_id   INTEGER         NOT NULL,
                                       winner_id       INTEGER         NOT NULL,
                                       match_time      TIMESTAMP       DEFAULT CURRENT_TIMESTAMP,
                                       FOREIGN KEY (player_one_id) REFERENCES users(id),
    FOREIGN KEY (player_two_id) REFERENCES users(id),
    FOREIGN KEY (winner_id) REFERENCES users(id)
    );

CREATE VIEW leaderboard AS
SELECT username, elo, wins, losses
FROM users
ORDER BY elo DESC, wins DESC;

CREATE OR REPLACE FUNCTION update_user_stats()
    RETURNS TRIGGER AS $$
BEGIN
    -- Update stats for the winner
    UPDATE users
    SET wins = wins + 1,
        elo = elo + 3
    WHERE id = NEW.winner_id;

    -- Update stats for the loser
    UPDATE users
    SET losses = losses + 1,
        elo = CASE
                  WHEN elo - 5 < 0 THEN 0 -- Prevent negative ELO
                  ELSE elo - 5
            END
    WHERE id IN (NEW.player_one_id, NEW.player_two_id) AND id != NEW.winner_id;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER after_match_played
    AFTER INSERT ON matches
    FOR EACH ROW
EXECUTE FUNCTION update_user_stats();