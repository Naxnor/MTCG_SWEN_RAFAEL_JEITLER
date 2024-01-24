﻿using MTCG.Controller;
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
                // Use the NpgsqlDbType.Uuid to specify the type of the parameter explicitly.
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
}