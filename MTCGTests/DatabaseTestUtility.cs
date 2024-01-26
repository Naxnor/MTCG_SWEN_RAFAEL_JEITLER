
using NUnit.Framework;
using MTCG.Database.Repository;
using System;
using System.Text;
using MTCG.Database;
using MTCG.Models;
using Npgsql;
using MTCGTesting.Utilities;
using MTCG.Database;
using MTCG.Database.Repository;
using MTCG.Models;
using Npgsql;

namespace MTCGTesting.Utilities
{
    public static class DatabaseTestUtility
    {
        private static CardRepository _cardRepository;
        
  
        
        public static void ResetDatabase()
        {
            using (var conn = new NpgsqlConnection(DBManager.ConnectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = @"
                    TRUNCATE TABLE users RESTART IDENTITY CASCADE;
                    TRUNCATE TABLE Cards RESTART IDENTITY CASCADE;
                    TRUNCATE TABLE UserCards RESTART IDENTITY CASCADE;
                    TRUNCATE TABLE Packages RESTART IDENTITY CASCADE;
                    TRUNCATE TABLE CardPackage RESTART IDENTITY CASCADE;
                    TRUNCATE TABLE trading_deals RESTART IDENTITY CASCADE;";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void CreateTestUsers()
        {
            using (var conn = new NpgsqlConnection(DBManager.ConnectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = @"
                    INSERT INTO users (username, password) VALUES ('testUser1', 'testPassword1');
                    INSERT INTO users (username, password) VALUES ('testUser2', 'testPassword2');";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void CleanUpDatabase()
        {
            // This method can be identical to ResetDatabase if you want to completely clear the test data
            ResetDatabase();
        }
        
        public static void EnsureTestCardExists()
        {
           
            var cardRepository = new CardRepository();

            Guid testCardId = new Guid("70962948-2bf7-44a9-9ded-8c68eeac7793");

            if (!cardRepository.DoesCardExist(testCardId))
            {
                var testCard = new Card
                {
                    Id = testCardId,
                    Name = "Test Card",
                    Damage = 10.0f,
                    Element = "Fire",
                    Class = "Dragon",
                    Type = "Monster"
                };
                cardRepository.AddCard(testCard);
            }
        }
        public static void EnsureTestUsersAndCardsExist()
        {
            // Create test users and cards
            // Note: Implement CreateTestUser and CreateTestCard methods based on your database schema
            CreateTestUser(1, "User1");
            CreateTestUser(2, "User2");

            var cardIds = new List<Guid>
            {
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid()
            };
            var cardIds2 = new List<Guid>
            {
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid()
            };
// Create cards and add them to the user's deck
            foreach (var cardId in cardIds)
            {
                var testCard = new Card
                {
                    Id = cardId,
                    Name = "Test Card",
                    Damage = 10.0f,
                    Element = "Fire",
                    Class = "Dragon",
                    Type = "Monster"
                };
                CreateTestCard(testCard);
            }

// Assuming userId is the ID of the test user
            AddCardsToUserDeck(1, cardIds);
            foreach (var cardId in cardIds2)
            {
                var testCard = new Card
                {
                    Id = cardId,
                    Name = "Test Card",
                    Damage = 10.0f,
                    Element = "Fire",
                    Class = "Dragon",
                    Type = "Monster"
                };
                CreateTestCard(testCard);
            }

// Assuming userId is the ID of the test user
            AddCardsToUserDeck(2, cardIds2);
        }

        public static void CreateTestUser(int userId, string username)
        {
            using (var conn = new NpgsqlConnection(DBManager.ConnectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "INSERT INTO users (id, username, password) VALUES (@id, @username, 'testPassword')";
                    cmd.Parameters.AddWithValue("@id", userId);
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.ExecuteNonQuery();
                }
            }
        }


        public static void CreateTestCard(Card card)
        {
            using (var conn = new NpgsqlConnection(DBManager.ConnectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "INSERT INTO Cards (Id, Name, Damage, Element, Class, Type) VALUES (@Id, @Name, @Damage, @Element, @Class, @Type)";
                    cmd.Parameters.AddWithValue("@Id", card.Id);
                    cmd.Parameters.AddWithValue("@Name", card.Name);
                    cmd.Parameters.AddWithValue("@Damage", card.Damage);
                    cmd.Parameters.AddWithValue("@Element", card.Element);
                    cmd.Parameters.AddWithValue("@Class", card.Class);
                    cmd.Parameters.AddWithValue("@Type", card.Type);
                    cmd.ExecuteNonQuery();
                }
            }
        }


        private static void AddCardsToUserDeck(int userId, IEnumerable<Guid> cardIds)
        {
            using (var conn = new NpgsqlConnection(DBManager.ConnectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = conn;
                    foreach (var cardId in cardIds)
                    {
                        cmd.CommandText = "INSERT INTO UserCards (UserId, CardId, InDeck) VALUES (@UserId, @CardId, TRUE)";
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.Parameters.AddWithValue("@CardId", cardId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }


        public static Guid CreateTestTradingDeal(int userId, string type, float minimumDamage)
        {
            using (var conn = new NpgsqlConnection(DBManager.ConnectionString))
            {
                conn.Open();

                // Create a test card first
                var cardId = Guid.NewGuid();
                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = @"
                INSERT INTO Cards (Id, Name, Damage, Element, Class, Type) 
                VALUES (@Id, 'Test Card', 50.0, 'Fire', 'Dragon', 'Monster')";
                    cmd.Parameters.AddWithValue("@Id", cardId);
                    cmd.ExecuteNonQuery();
                }

                // Then create the trading deal
                var dealId = Guid.NewGuid();
                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = @"
                INSERT INTO trading_deals (id, UserId, cardtotrade, type, MinimumDamage)
                VALUES (@Id, @UserId, @CardToTrade, @Type, @MinimumDamage)";
                    cmd.Parameters.AddWithValue("@Id", dealId);
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@CardToTrade", cardId);
                    cmd.Parameters.AddWithValue("@Type", type);
                    cmd.Parameters.AddWithValue("@MinimumDamage", minimumDamage);
                    cmd.ExecuteNonQuery();
                }

                return dealId;
            }
        }


        public static void CleanUpTradingDeals()
        {
            using (var conn = new NpgsqlConnection(DBManager.ConnectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("DELETE FROM trading_deals", conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }
        
    }

       
    }

   
