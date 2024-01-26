using MTCG.Controller;
using MTCG.Models;
using Npgsql;
using NpgsqlTypes;

namespace MTCG.Database.Repository;

public class TransactionRepository
{
    public bool DoesTradingDealExist(Guid Id)
    {
        
        using (var conn = new NpgsqlConnection(DBManager.ConnectionString))
        {
            conn.Open();

            using (NpgsqlCommand cmd = new NpgsqlCommand("SELECT COUNT(*) FROM trading_deals WHERE id = @id", conn))
            {
               
                cmd.Parameters.Add(new NpgsqlParameter("id", NpgsqlDbType.Uuid) { Value = Id });

                int count = Convert.ToInt32(cmd.ExecuteScalar());

                return count > 0;
            }
        }
    }



    public bool CreateTradingDeal(TradingDeal tradingDeal, int UserId)
    {
        
        string insertQuery = @"
            INSERT INTO trading_deals (id, UserId, cardtotrade, type, MinimumDamage)
            VALUES (@Id, @UserId, @CardToTrade, @Type, @MinimumDamage)";

        using (var conn = new NpgsqlConnection(DBManager.ConnectionString))
        using (var cmd = new NpgsqlCommand(insertQuery, conn))
        {
            cmd.Parameters.AddWithValue("@Id", tradingDeal.Id);
            cmd.Parameters.AddWithValue("@UserId", UserId); 
            cmd.Parameters.AddWithValue("@CardToTrade", tradingDeal.CardToTrade);
            cmd.Parameters.AddWithValue("@Type", tradingDeal.Type);
            cmd.Parameters.AddWithValue("@MinimumDamage", tradingDeal.MinimumDamage);

            conn.Open();
            int affectedRows = cmd.ExecuteNonQuery();
            return affectedRows == 1; // Return true if one row was affected, meaning the insert was successful
        }
    }

    public List<TradingDeal> GetAllTradingDeals()
    {
        var deals = new List<TradingDeal>();
        const string query = "SELECT * FROM trading_deals";

        using (var conn = new NpgsqlConnection(DBManager.ConnectionString))
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand(query, conn))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var deal = new TradingDeal
                        {
                            Id = reader.GetGuid(reader.GetOrdinal("Id")),
                            CardToTrade = reader.GetGuid(reader.GetOrdinal("CardToTrade")),
                            Type = reader.GetString(reader.GetOrdinal("Type")),
                            MinimumDamage = reader.GetFloat(reader.GetOrdinal("MinimumDamage")),
                            UserId = reader.GetInt32(reader.GetOrdinal("userid"))
                        };
                        deals.Add(deal);
                    }
                }
            }
        }
        return deals;
    }

    public bool IsTradingDealOwnedByUser(Guid tradingDealId, int userId)
    {
        string query = @"
        SELECT COUNT(*)
        FROM trading_deals
        WHERE id = @TradingDealId AND UserId = @UserId";
        
        using (var conn = new NpgsqlConnection(DBManager.ConnectionString))
        using (var cmd = new NpgsqlCommand(query, conn))
        {
            cmd.Parameters.AddWithValue("@TradingDealId", tradingDealId);
            cmd.Parameters.AddWithValue("@UserId", userId);
        
            conn.Open();
            int count = Convert.ToInt32(cmd.ExecuteScalar());
        
            return count > 0; // If the count is greater than 0, the trading deal is owned by the user
        }
    }

    public bool DeleteTradingDeal(Guid tradingDealId)
    {
        string query = @"
        DELETE FROM trading_deals
        WHERE id = @TradingDealId";
        
        using (var conn = new NpgsqlConnection(DBManager.ConnectionString))
        using (var cmd = new NpgsqlCommand(query, conn))
        {
            cmd.Parameters.AddWithValue("@TradingDealId", tradingDealId);
        
            conn.Open();
            int affectedRows = cmd.ExecuteNonQuery();
        
            return affectedRows > 0; // If the affected rows are greater than 0, the trading deal was successfully deleted
        }
    }

    public TradingDeal GetTradingDeal(Guid tradingDealId)
    {
        const string query = @"
        SELECT * FROM trading_deals
        WHERE id = @TradingDealId";

        using (var conn = new NpgsqlConnection(DBManager.ConnectionString))
        using (var cmd = new NpgsqlCommand(query, conn))
        {
            cmd.Parameters.AddWithValue("@TradingDealId", tradingDealId);

            conn.Open();
            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    return new TradingDeal
                    {
                        Id = reader.GetGuid(reader.GetOrdinal("id")),
                        CardToTrade = reader.GetGuid(reader.GetOrdinal("CardToTrade")),
                        Type = reader.GetString(reader.GetOrdinal("Type")),
                        MinimumDamage = reader.GetFloat(reader.GetOrdinal("MinimumDamage")),
                        UserId = reader.GetInt32(reader.GetOrdinal("UserId"))
                    };
                }
            }
        }
        return null; 
    }

    public bool DoesCardMeetTradeRequirements(Guid offeredCardId, TradingDeal tradingDeal)
    {
        const string query = @"
        SELECT Damage, Type FROM Cards
        WHERE Id = @CardId";

        using (var conn = new NpgsqlConnection(DBManager.ConnectionString))
        using (var cmd = new NpgsqlCommand(query, conn))
        {
            cmd.Parameters.AddWithValue("@CardId", offeredCardId);

            conn.Open();
            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    float damage = reader.GetFloat(reader.GetOrdinal("Damage"));
                    string type = reader.GetString(reader.GetOrdinal("Type"));

                    
                    return damage >= tradingDeal.MinimumDamage && type.Equals(tradingDeal.Type, StringComparison.OrdinalIgnoreCase);
                }
            }
        }
        return false; 
    }

    public bool ExecuteTradeAndDeleteDeal(Guid offeredCardId, Guid tradingDealId, int userId)
    {
        const string tradeQuery = @"
        BEGIN;
        UPDATE UserCards
        SET UserId = @NewOwnerId
        WHERE CardId = @CardToTrade;

        UPDATE UserCards
        SET UserId = @OriginalOwnerId
        WHERE CardId = @OfferedCardId;

        DELETE FROM trading_deals
        WHERE id = @TradingDealId;
        COMMIT;";

        using (var conn = new NpgsqlConnection(DBManager.ConnectionString))
        using (var cmd = new NpgsqlCommand(tradeQuery, conn))
        {
            // Retrieve the original owner of the card to trade
            var originalOwnerId = GetTradingDeal(tradingDealId)?.UserId;
            if (originalOwnerId == null) return false;

            cmd.Parameters.AddWithValue("@NewOwnerId", userId);
            cmd.Parameters.AddWithValue("@OriginalOwnerId", originalOwnerId);
            cmd.Parameters.AddWithValue("@CardToTrade", tradingDealId);
            cmd.Parameters.AddWithValue("@OfferedCardId", offeredCardId);
            cmd.Parameters.AddWithValue("@TradingDealId", tradingDealId);

            conn.Open();
            int affectedRows = cmd.ExecuteNonQuery();

            return affectedRows > 0;
        }
    }

}