using System.Collections;
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
                        Damage = reader.GetFloat(reader.GetOrdinal("Damage")),
                        Element = reader.GetString(reader.GetOrdinal("Element")),
                        Class = reader.GetString(reader.GetOrdinal("Class")),
                        Type = reader.GetString(reader.GetOrdinal("Type"))
                    };
                }
            }
        }
        return null;
    }

   
    public bool AddCard(Card card)
    {

        var (element, @class, type) = ExtractElementClassAndType(card.Name);

        string insertQuery = "INSERT INTO Cards (Id, Name, Damage, Element, Class,Type) VALUES (@Id, @Name, @Damage, @Element, @Class, @Type)";
        using (var conn = new NpgsqlConnection(DBManager.ConnectionString))

        {
            conn.Open();
            using (NpgsqlCommand cmd = new NpgsqlCommand(insertQuery, conn))
            {
                cmd.Parameters.AddWithValue("@Id", card.Id);
                cmd.Parameters.AddWithValue("@Name", card.Name);
                cmd.Parameters.AddWithValue("@Damage", card.Damage);
                cmd.Parameters.AddWithValue("@Element", element);
                cmd.Parameters.AddWithValue("@Class", @class);
                cmd.Parameters.AddWithValue("@Type",type);


                int affectedRows = cmd.ExecuteNonQuery();
                return affectedRows > 0;
            }
        }
    }

    public (string Element, string Class, string Type) ExtractElementClassAndType(string cardName)
    {
        string element = cardName.Contains("Water") ? "Water" :
            cardName.Contains("Fire") ? "Fire" :
            cardName.Contains("Air") ? "Air" :
            cardName.Contains("Ice") ? "Ice" :
            cardName.Contains("Plant") ? "Plant" :
            cardName.Contains("Electro") ? "Electro" :
            cardName.Contains("Ground") ? "Ground" :
            "Regular"; // Default element if no other matches

        string @class = cardName.Contains("Dragon") ? "Dragon" :
            cardName.Contains("Goblin") ? "Goblin" :
            cardName.Contains("Spell") ? "Spell" :
            cardName.Contains("Ork") ? "Ork" :
            cardName.Contains("Wizzard") ? "Wizzard" :
            cardName.Contains("Knight") ? "Knight" :
            cardName.Contains("Kraken") ? "Kraken" :
            cardName.Contains("Trap") ? "Trap" :
            cardName.Contains("Elf") ? "Elf" :
            cardName.Contains("Vampire") ? "Vampire" :
            cardName.Contains("Dwarf") ? "Dwarf" :
            cardName.Contains("Troll") ? "Troll" :
            "Monster"; // Default class if no other matches

        string type = (@class == "Spell" || @class == "Trap") ? "Spell" : "Monster"; // Type logic

        return (element, @class, type);
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

        var (element, @class,type) = ExtractElementClassAndType(card.Name);
        string insertQuery = "INSERT INTO Cards (Id, Name, Damage, Element, Class,Type) VALUES (@Id, @Name, @Damage, @Element, @Class,@Type)";

        using (var cmd = new NpgsqlCommand(insertQuery, conn, trans))
        {
            cmd.Parameters.AddWithValue("@Id", card.Id);
            cmd.Parameters.AddWithValue("@Name", card.Name);
            cmd.Parameters.AddWithValue("@Damage", card.Damage);
            cmd.Parameters.AddWithValue("@Element", element);
            cmd.Parameters.AddWithValue("@Class", @class);
            cmd.Parameters.AddWithValue("@Type", type);
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
                            Damage = reader.GetFloat(reader.GetOrdinal("Damage")),
                            Element = reader.GetString(reader.GetOrdinal("Element")),
                            Class = reader.GetString(reader.GetOrdinal("Class")),
                            Type = reader.GetString(reader.GetOrdinal("Type"))
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
            SELECT c.Id, c.Name, c.Damage ,c.element ,c.class , c.type
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
                            Damage = reader.GetFloat(reader.GetOrdinal("Damage")),
                            Element = reader.GetString(reader.GetOrdinal("Element")),
                            Class = reader.GetString(reader.GetOrdinal("Class")),
                            Type = reader.GetString(reader.GetOrdinal("Type"))
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
        SELECT c.Id, c.Name, c.Damage ,c.element ,c.class , c.type
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
                            Element = reader.GetString(reader.GetOrdinal("Element")),
                            Damage = reader.GetFloat(reader.GetOrdinal("Damage")),
                            Class = reader.GetString(reader.GetOrdinal("Class")),
                            Type = reader.GetString(reader.GetOrdinal("Type"))
                            
                            
                        });
                    }
                }
            }
        }
        return deck;
    }

    public IEnumerable<Card> GetCardsInDeck(int userId)
    {
        var cards = new List<Card>();
        var query = "SELECT c.* FROM Cards c INNER JOIN UserCards uc ON c.Id = uc.CardId WHERE uc.UserId = @UserId AND uc.InDeck = TRUE";

        using (var conn = new NpgsqlConnection(DBManager.ConnectionString))
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        cards.Add(new Card
                        {
                            Id = reader.GetGuid(reader.GetOrdinal("Id")),
                            Name = reader.GetString(reader.GetOrdinal("Name")),
                            Damage = reader.GetFloat(reader.GetOrdinal("Damage")),
                            Class = reader.GetString(reader.GetOrdinal("Class")),
                            Element = reader.GetString(reader.GetOrdinal("Element")),
                            
                            // Populate other properties if they exist
                        });
                    }
                }
            }
        }

        return cards;
    }

    public bool IsCardOwnedAndNotInDeck(Guid cardId, int userId)
    {
        Console.WriteLine($"Checking ownership for card ID: {cardId} and user ID: {userId}");

        string query = @"
        SELECT COUNT(*)
        FROM UserCards
        WHERE CardId = @CardId AND UserId = @userId AND InDeck = FALSE";
        
        using (var conn = new NpgsqlConnection(DBManager.ConnectionString))
        using (var cmd = new NpgsqlCommand(query, conn))
        {
            cmd.Parameters.AddWithValue("@CardId", cardId);
            cmd.Parameters.AddWithValue("@UserId", userId);
        
            conn.Open();
        
            int count = Convert.ToInt32(cmd.ExecuteScalar());
            bool isOwnedAndNotInDeck = count > 0;

            Console.WriteLine($"Card owned and not in deck: {isOwnedAndNotInDeck}");

            return isOwnedAndNotInDeck; // If the count is greater than 0, the user owns the card and it's not in a deck
        }
    }

    public Guid GetOldestPackageId()
    {
        using (var conn = new NpgsqlConnection(DBManager.ConnectionString))
        {
            conn.Open();
        
            using (var cmd = new NpgsqlCommand("SELECT PackageId FROM Packages ORDER BY CreatedTimestamp ASC LIMIT 1", conn))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return reader.GetGuid(0); // Assuming the PackageId column is of type GUID
                    }
                    else
                    {
                        // No packages found in the database, return Guid.Empty or handle it as needed
                        return Guid.Empty;
                    }
                }
            }
        }
    }


    public void DeleteCard(Guid cardId)
    {
        string deleteQuery = "DELETE FROM Cards WHERE Id = @Id";

        using (var conn = new NpgsqlConnection(DBManager.ConnectionString))
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand(deleteQuery, conn))
            {
                cmd.Parameters.AddWithValue("@Id", cardId);
                cmd.ExecuteNonQuery();
            }
        }
    }

    public Guid GetNewestPackageId()
    {
        using (var conn = new NpgsqlConnection(DBManager.ConnectionString))
        {
            conn.Open();
        
            using (var cmd = new NpgsqlCommand("SELECT PackageId FROM Packages ORDER BY CreatedTimestamp DESC LIMIT 1", conn))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return reader.GetGuid(0); // Assuming the PackageId column is of type GUID
                    }
                    else
                    {
                        // No packages found in the database, return Guid.Empty or handle it as needed
                        return Guid.Empty;
                    }
                }
            }
        }
    }

}