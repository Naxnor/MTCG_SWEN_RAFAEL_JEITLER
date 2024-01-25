﻿using System.Collections;
using MTCG.Models;
using Npgsql;

namespace MTCG.Database.Repository;


public class CardRepository
{
    public Card GetCardById(Guid id)
    {
        string selectQuery = "SELECT * FROM Cards WHERE Id = @id";
        using (NpgsqlConnection conn = new NpgsqlConnection(DBManager.ConnectionString))
        using (NpgsqlCommand cmd = new NpgsqlCommand(selectQuery, conn))
        {
            cmd.Parameters.AddWithValue("@id", id);
            conn.Open();
            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    return new Card
                    {
                        Id = reader.GetGuid(reader.GetOrdinal("Id")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        Damage = reader.GetFloat(reader.GetOrdinal("Damage"))
                    };
                }
            }
        }
        return null;
    }

   
    public bool AddCard(Card card)
    {
        string insertQuery = "INSERT INTO Cards (Id, Name, Damage) VALUES (@Id, @Name, @Damage)";
        using (NpgsqlConnection conn = new NpgsqlConnection(DBManager.ConnectionString))
        {
            conn.Open();
            using (NpgsqlCommand cmd = new NpgsqlCommand(insertQuery, conn))
            {
                cmd.Parameters.AddWithValue("@Id", card.Id);
                cmd.Parameters.AddWithValue("@Name", card.Name);
                cmd.Parameters.AddWithValue("@Damage", card.Damage);

                int affectedRows = cmd.ExecuteNonQuery();
                return affectedRows > 0;
            }
        }
    }


    public bool AddPackage(IEnumerable<Card> cards)
{
    var packageId = Guid.NewGuid();
    
    using (var conn = new NpgsqlConnection(DBManager.ConnectionString))
    {
        try
        {
            conn.Open();
            using (var trans = conn.BeginTransaction())
            {
                var packageInsertQuery = "INSERT INTO Packages (PackageId) VALUES (@PackageId)";
                using (var cmd = new NpgsqlCommand(packageInsertQuery, conn, trans))
                {
                    cmd.Parameters.AddWithValue("@PackageId", packageId);
                    cmd.ExecuteNonQuery();
                    Console.WriteLine($"Package created with ID: {packageId}");
                }

                foreach (var card in cards)
                {
                    Console.WriteLine($"Adding card: {card.Id}");
                    if (!AddCard(card, conn, trans))
                    {
                        Console.WriteLine("Failed to add card to Cards table");
                        trans.Rollback();
                        return false;
                    }

                    var linkInsertQuery = "INSERT INTO CardPackage (CardId, PackageId) VALUES (@CardId, @PackageId)";
                    using (var linkCmd = new NpgsqlCommand(linkInsertQuery, conn, trans))
                    {
                        linkCmd.Parameters.AddWithValue("@CardId", card.Id);
                        linkCmd.Parameters.AddWithValue("@PackageId", packageId);
                        int linkRowsAffected = linkCmd.ExecuteNonQuery();
                        if (linkRowsAffected == 0)
                        {
                            Console.WriteLine("Failed to add entry to CardPackage table");
                            trans.Rollback();
                            return false;
                        }
                    }
                }

                trans.Commit();
                Console.WriteLine("Transaction committed successfully");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error in AddPackage: " + ex.Message);
            return false;
        }
    }
    return true;
}



    private bool AddCard(Card card, NpgsqlConnection conn, NpgsqlTransaction trans)
    {
        string insertQuery = "INSERT INTO cards (Id, Name, Damage) VALUES (@Id, @Name, @Damage)";
        using (var cmd = new NpgsqlCommand(insertQuery, conn, trans))
        {
            cmd.Parameters.AddWithValue("@Id", card.Id);
            cmd.Parameters.AddWithValue("@Name", card.Name);
            cmd.Parameters.AddWithValue("@Damage", card.Damage);

            int affectedRows = cmd.ExecuteNonQuery();
            return affectedRows > 0;
        }
    }

    public bool DoesCardExist(Guid cardId)
    {
        string selectQuery = "SELECT COUNT(1) FROM Cards WHERE Id = @Id";
    
        using (var conn = new NpgsqlConnection(DBManager.ConnectionString))
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand(selectQuery, conn))
            {
                cmd.Parameters.AddWithValue("@Id", cardId);
            
                // ExecuteScalar returns the first column of the first row in the result set
                int count = Convert.ToInt32(cmd.ExecuteScalar());
                return count > 0;
            }
        }
    }
    public void DeletePackage(Guid packageId)
    {
        string deleteCardPackageQuery = "DELETE FROM CardPackage WHERE PackageId = @PackageId";

        using (var conn = new NpgsqlConnection(DBManager.ConnectionString))
        {
            conn.Open();
            using (var trans = conn.BeginTransaction())
            {
                // Delete referencing rows in CardPackage
                using (var cmd = new NpgsqlCommand(deleteCardPackageQuery, conn, trans))
                {
                    cmd.Parameters.AddWithValue("@PackageId", packageId);
                    cmd.ExecuteNonQuery();
                }

                // Now, delete the package
                var deletePackageQuery = "DELETE FROM Packages WHERE PackageId = @PackageId";
                using (var cmd = new NpgsqlCommand(deletePackageQuery, conn, trans))
                {
                    cmd.Parameters.AddWithValue("@PackageId", packageId);
                    cmd.ExecuteNonQuery();
                }

                trans.Commit();
            }
        }
    }

    public Guid GetRandomPackageId()
    {
        string selectQuery = "SELECT PackageId FROM Packages ORDER BY RANDOM() LIMIT 1";

        using (var conn = new NpgsqlConnection(DBManager.ConnectionString))
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand(selectQuery, conn))
            {
                var packageId = cmd.ExecuteScalar();
                return packageId != null ? (Guid)packageId : Guid.Empty;
            }
        }
    }
    public IEnumerable<Card> GetCardsByPackageId(Guid packageId)
    {
        var cards = new List<Card>();
        string selectQuery = "SELECT c.* FROM Cards c INNER JOIN CardPackage cp ON c.Id = cp.CardId WHERE cp.PackageId = @PackageId";

        using (var conn = new NpgsqlConnection(DBManager.ConnectionString))
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand(selectQuery, conn))
            {
                cmd.Parameters.AddWithValue("@PackageId", packageId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        cards.Add(new Card
                        {
                            Id = reader.GetGuid(reader.GetOrdinal("Id")),
                            Name = reader.GetString(reader.GetOrdinal("Name")),
                            Damage = reader.GetFloat(reader.GetOrdinal("Damage"))
                        });
                    }
                }
            }
        }
        return cards;
    }
    
    public IEnumerable<CardDTO> GetUserCards(int userId)
    {
        var cards = new List<CardDTO>();
        string selectQuery = @"
            SELECT c.Id, c.Name, c.Damage 
            FROM Cards c
            INNER JOIN UserCards uc ON c.Id = uc.CardId
            WHERE uc.UserId = @UserId";

        using (var conn = new NpgsqlConnection(DBManager.ConnectionString))
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand(selectQuery, conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        cards.Add(new CardDTO
                        {
                            Id = reader.GetGuid(reader.GetOrdinal("Id")),
                            Name = reader.GetString(reader.GetOrdinal("Name")),
                            Damage = reader.GetFloat(reader.GetOrdinal("Damage"))
                        });
                    }
                }
            }
        }
        return cards;
    }
    public bool ConfigureDeck(int userId, IEnumerable<Guid> cardIds)
    {
        var inDeckStatusUpdateQuery = @"
            UPDATE UserCards
            SET InDeck = CASE
                WHEN CardId = ANY(@CardIds) THEN TRUE
                ELSE FALSE
            END
            WHERE UserId = @UserId";

        using (var conn = new NpgsqlConnection(DBManager.ConnectionString))
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand(inDeckStatusUpdateQuery, conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@CardIds", cardIds.ToArray());
                cmd.ExecuteNonQuery();
            }
        }
        return true;
    }
    public IEnumerable<CardDTO> GetUserDeck(int userId)
    {
        var deck = new List<CardDTO>();
        string selectQuery = @"
        SELECT c.Id, c.Name, c.Damage
        FROM Cards c
        INNER JOIN UserCards uc ON c.Id = uc.CardId
        WHERE uc.UserId = @UserId AND uc.InDeck = TRUE";

        using (var conn = new NpgsqlConnection(DBManager.ConnectionString))
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand(selectQuery, conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        deck.Add(new CardDTO
                        {
                            Id = reader.GetGuid(reader.GetOrdinal("Id")),
                            Name = reader.GetString(reader.GetOrdinal("Name")),
                            Damage = reader.GetFloat(reader.GetOrdinal("Damage"))
                        });
                    }
                }
            }
        }
        return deck;
    }
}