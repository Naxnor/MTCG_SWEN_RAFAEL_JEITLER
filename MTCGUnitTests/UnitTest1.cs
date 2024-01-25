using NUnit.Framework;
using MTCG.Database.Repository;
using System;
using System.Text;
using MTCG.Models;

namespace MTCGTesting
{
    [TestFixture]
    public class UserRepositoryIntegrationTests
    {
        private UserRepository _userRepository;

        [SetUp]
        public void SetUp()
        {
            // Initialize UserRepository
            _userRepository = new UserRepository();
        }
        [Test]
        public void CreateUser_AddsNewUserToDatabase()
        {
            // Arrange
            var newUser = new User
            {
                Username = $"testUser_{Guid.NewGuid()}", // Ensure the username is unique
                Password = "testPassword"
            };

            try
            {
                // Act
                _userRepository.CreateUser(newUser);

                // Assert
                var createdUser = _userRepository.GetUserByUsername(newUser.Username);
                Assert.IsNotNull(createdUser);
                Assert.AreEqual(newUser.Username, createdUser.Username);
            }
            finally
            {
                // Cleanup
                _userRepository.DeleteUser(newUser.Username);
            }
        }

        [Test]
        public void CreateUser_AddsTestUserToDatabase()
        {
            // Arrange
            var newUser = new User
            {
                Username = "existingUser", // not unique 
                Password = "testPassword"
            };

            // Act
            _userRepository.CreateUser(newUser);

            // Assert
            var createdUser = _userRepository.GetUserByUsername(newUser.Username);
            Assert.IsNotNull(createdUser);
            Assert.AreEqual(newUser.Username, createdUser.Username);

            
        } 
        [Test]
        public void AuthenticateUser_WithExistingUser_ReturnsTrue()
        {
            // Arrange
            // Ensure these credentials exist in your test database
            string testUsername = "admin";
            string testPassword = "istrator";

            // Act
            bool result = _userRepository.AuthenticateUser(testUsername, testPassword);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void AuthenticateUser_WithWrongPassword_ReturnsFalse()
        {
            // Arrange
            // Ensure this user exists but with a different password
            string testUsername = "existingUser";
            string wrongPassword = "wrongPassword";

            // Act
            bool result = _userRepository.AuthenticateUser(testUsername, wrongPassword);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void DeleteUser_RemovesUserFromDatabase()
        {
            // Arrange
            var testUsername = $"testUser_{Guid.NewGuid()}"; // Unique username
            var testUser = new User
            {
                Username = testUsername,
                Password = "testPassword"
            };

            // Create a test user first
            _userRepository.CreateUser(testUser);

            // Act
            _userRepository.DeleteUser(testUsername);

            // Assert
            var userAfterDeletion = _userRepository.GetUserByUsername(testUsername);
            Assert.IsNull(userAfterDeletion, "User should be null after deletion");
        }
        [Test]
        public void DeleteUser_RemovesTestUserFromDatabase()
        {
            // Arrange
            var testUsername = "existingUser"; 
            var testUser = new User
            {
                Username = testUsername,
                Password = "testPassword"
            };
            
            // Act
            _userRepository.DeleteUser(testUsername);

            // Assert
            var userAfterDeletion = _userRepository.GetUserByUsername(testUsername);
            Assert.IsNull(userAfterDeletion, "User should be null after deletion");
        }
   
        [Test]
        public void UpdateUser_UpdatesExistingUser()
        {
            // Arrange
            var testUsername = $"testUser_{Guid.NewGuid()}"; // Unique username
            var newUser = new User
            {
                Username = testUsername,
                Password = "testPassword"
            };
            _userRepository.CreateUser(newUser);

            var updatedUserData = new User
            {
                Name = "Updated Name",
                Bio = "Updated Bio",
                Image = "Updated Image"
            };

            // Act
            var result = _userRepository.UpdateUser(testUsername, updatedUserData);

            // Assert
            Assert.IsTrue(result);

            // Cleanup
            _userRepository.DeleteUser(testUsername);
        }
        [Test]
        public void AddCoins_IncreasesUserCoinBalance()
        {
            // Arrange
            var userId = 11; // Use a test user ID that exists in your database
            var amountToAdd = 100;

            // Act
            var result = _userRepository.AddCoins(userId, amountToAdd);

            // Assert
            Assert.IsTrue(result);
        }



    }

    [TestFixture]
    public class CardRepositoryIntegrationTests
    {
        
        private CardRepository _cardRepository;

        [SetUp]
        public void SetUp()
        {
            // Initialize UserRepository
            _cardRepository = new CardRepository();
        }
        [Test]
        public void AddCard_AddsNewCardToDatabase()
        {
            // Arrange
            var newCard = new Card
            {
                Id = Guid.NewGuid(),
                Name = "Test Fire Dragon",
                Damage = 50.0f,
                Element = "Fire",
                Class = "Dragon",
                Type = "Monster"
            };

            // Act
            bool result = _cardRepository.AddCard(newCard);

            // Assert
            Assert.IsTrue(result);

            // Cleanup: Delete the added card
            _cardRepository.DeleteCard(newCard.Id);
        }
        [Test]
        public void GetCardById_RetrievesCard()
        {
            // Arrange
            // Assuming you have a known Card ID in your database
            Guid knownCardId = new Guid("70962948-2bf7-44a9-9ded-8c68eeac7793");

            // Act
            Card card = _cardRepository.GetCardById(knownCardId);

            // Assert
            Assert.IsNotNull(card);
            Assert.AreEqual(knownCardId, card.Id);
        }
        [Test]
        public void DoesCardExist_WithExistingCard_ReturnsTrue()
        {
            // Arrange
            // Assuming you have a known Card ID in your database
            Guid existingCardId = new Guid("70962948-2bf7-44a9-9ded-8c68eeac7793");

            // Act
            bool result = _cardRepository.DoesCardExist(existingCardId);

            // Assert
            Assert.IsTrue(result);
        }
        [Test]
        public void AddPackage_AddsNewPackageWithCards()
        {
            // Arrange
            var cards = new List<Card>
            {
                new Card { Id = Guid.NewGuid(), Name = "Test Card 1", Damage = 10.0f },
                new Card { Id = Guid.NewGuid(), Name = "Test Card 2", Damage = 20.0f },
                new Card { Id = Guid.NewGuid(), Name = "Test Card 3", Damage = 30.0f },
                new Card { Id = Guid.NewGuid(), Name = "Test Card 4", Damage = 40.0f }
            };

            // Act
            bool result = _cardRepository.AddPackage(cards);

            // Assert
            Assert.IsTrue(result);

            // Cleanup
            var packageId = _cardRepository.GetNewestPackageId(); // Assuming you have a method to get the oldest package ID
            _cardRepository.DeletePackage(packageId); // This should handle deleting entries in CardPackage

            foreach (var card in cards)
            {
                _cardRepository.DeleteCard(card.Id); // Delete the test cards
            }
        }
        [Test]
        public void GetUserDeck_RetrievesUsersDeck()
        {
            // Arrange
            int testUserId = 10; // Assuming this user ID exists in your test database
            var expectedDeck = _cardRepository.GetUserDeck(testUserId).ToList();

            // Act
            var actualDeck = _cardRepository.GetUserDeck(testUserId).ToList();

            // Assert
            Assert.IsNotNull(actualDeck);
            Assert.AreEqual(expectedDeck.Count, actualDeck.Count);

            // Optionally: Further validate each card in the deck
            for (int i = 0; i < expectedDeck.Count; i++)
            {
                Assert.AreEqual(expectedDeck[i].Id, actualDeck[i].Id);
                Assert.AreEqual(expectedDeck[i].Name, actualDeck[i].Name);
                // Other properties as needed...
            }
        }
    }
    
    [TestFixture]
    public class BattleServiceTests
    {
        private BattleService _battleService;

        [SetUp]
        public void Setup()
        {
            // Initialize BattleService here, potentially with mock dependencies if necessary
            _battleService = new BattleService();
        }

        [Test]
        public void EnterLobby_WithNewUser_AddsUserToLobby()
        {
            // Arrange
            int userId = 8; // Example user ID

            // Act
            int result = _battleService.EnterLobby(userId);

            // Assert
            Assert.AreNotEqual(0, result, "User should be added to the lobby and receive a valid opponent ID.");
        }

        [Test]
        public void StartBattle_WithValidUsers_StartsBattle()
        {
            // Arrange
            int userId = 1;
            string userName = "User1";
            int opponentId = 2;
            string opponentName = "User2";
            var userDeck = new List<Card> { /* Populate with test cards */ };
            var opponentDeck = new List<Card> { /* Populate with test cards */ };

            // Act
            var result = _battleService.StartBattle(userId, userName, opponentId, opponentName, userDeck, opponentDeck);

            // Assert
            Assert.IsNotEmpty(result, "Battle log should not be empty when a valid battle is started.");
        }

        [Test]
        public void ExecuteBattle_WithEqualDecks_EndsInDraw()
        {
            // Arrange
            int userId = 1;
            string userName = "User1";
            int opponentId = 2;
            string opponentName = "User2";
            var userDeck = new List<Card> { /* Populate with equally matched cards */ };
            var opponentDeck = new List<Card> { /* Populate with equally matched cards */ };

            // Act
            var battleLog = _battleService.ExecuteBattle(userId, userName, opponentId, opponentName, userDeck, opponentDeck);

            // Assert
            StringAssert.Contains("Battle ended in a draw", battleLog, "Battle should end in a draw with equally matched decks.");
        }

        [Test]
        public void SimulateRound_WithStrongerUserCard_UserWinsRound()
        {
            // Arrange
            var userCard = new Card { Damage = 50 }; // Stronger card
            var opponentCard = new Card { Damage = 30 }; // Weaker card

            // Act
            var outcome = _battleService.SimulateRound(userCard, opponentCard);

            // Assert
            Assert.AreEqual(BattleService.RoundResult.Win, outcome.Result, "User should win the round with a stronger card.");
        }

        [Test]
        public void CalculateEffectiveDamage_WithElementalAdvantage_IncreasesDamage()
        {
            // Arrange
            var attackingCard = new Card { Damage = 30, Element = "Fire" };
            var defendingCard = new Card { Element = "Plant" };

            // Act
            var damage = _battleService.CalculateEffectiveDamage(attackingCard, defendingCard);

            // Assert
            Assert.Greater(damage, attackingCard.Damage, "Damage should increase due to elemental advantage.");
        }

        [Test]
        public void UpdatePlayerStats_AfterBattle_UpdatesStatsCorrectly()
        {
            // Arrange
            int userId = 1;
            int opponentId = 2;
            int userWins = 5;
            int opponentWins = 3;
            var battleLog = new StringBuilder();

            // Act
            _battleService.UpdatePlayerStats(userId, opponentId, userWins, opponentWins, battleLog, 10);

            // Assert
            // This test might require checking the database or the returned battle log
            // As this is more of an integration test, exact asserts depend on the implementation
        }

        [Test]
        public void SaveBattleLog_WithBattleData_SavesLogCorrectly()
        {
            // Arrange
            int userId = 1;
            int opponentId = 2;
            var battleLog = new StringBuilder("Test Battle Log");

            // Act
            _battleService.SaveBattleLog(userId, opponentId, battleLog);

            // Assert
            // Check file system if the log file is created correctly
            // This is more of an integration test and might require file system access
        }

        [Test]
        public void ChooseCardForRound_WithNonEmptyDeck_ChoosesCard()
        {
            // Arrange
            var deck = new List<Card> { new Card(), new Card() }; // Non-empty deck

            // Act
            var chosenCard = _battleService.ChooseCardForRound(deck);

            // Assert
            Assert.IsNotNull(chosenCard, "A card should be chosen from a non-empty deck.");
        }
    }
    
    [TestFixture]
    public class TransactionRepositoryTests
    {
        private TransactionRepository _transactionRepository;

        [SetUp]
        public void Setup()
        {
            _transactionRepository = new TransactionRepository();
            // Setup any other necessary test data or configurations
        }

        [Test]
        public void DoesTradingDealExist_WithValidId_ReturnsTrue()
        {
            // Arrange
            Guid validDealId = new Guid("your-valid-deal-id");

            // Act
            bool result = _transactionRepository.DoesTradingDealExist(validDealId);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void CreateTradingDeal_WithValidData_CreatesDeal()
        {
            // Arrange
            var tradingDeal = new TradingDeal
            {
                Id = Guid.NewGuid(),
                // Set other properties of TradingDeal as required
            };
            int userId = 1; // Replace with a valid user ID

            // Act
            bool result = _transactionRepository.CreateTradingDeal(tradingDeal, userId);

            // Assert
            Assert.IsTrue(result);
            // Optionally, verify that the deal was actually created in the database
        }

        [Test]
        public void GetAllTradingDeals_ReturnsListOfDeals()
        {
            // Act
            var deals = _transactionRepository.GetAllTradingDeals();

            // Assert
            Assert.IsNotNull(deals);
            Assert.IsNotEmpty(deals);
        }

        [Test]
        public void IsTradingDealOwnedByUser_WithCorrectOwner_ReturnsTrue()
        {
            // Arrange
            Guid dealId = new Guid("your-deal-id");
            int ownerId = 1; // Replace with the correct owner ID

            // Act
            bool result = _transactionRepository.IsTradingDealOwnedByUser(dealId, ownerId);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void DeleteTradingDeal_WithExistingDeal_DeletesDeal()
        {
            // Arrange
            Guid dealId = new Guid("your-deal-id");

            // Act
            bool result = _transactionRepository.DeleteTradingDeal(dealId);

            // Assert
            Assert.IsTrue(result);
            // Optionally, verify that the deal was actually deleted from the database
        }

        [Test]
        public void GetTradingDeal_WithValidId_ReturnsDeal()
        {
            // Arrange
            Guid validDealId = new Guid("your-valid-deal-id");

            // Act
            var deal = _transactionRepository.GetTradingDeal(validDealId);

            // Assert
            Assert.IsNotNull(deal);
            Assert.AreEqual(validDealId, deal.Id);
        }

        [Test]
        public void DoesCardMeetTradeRequirements_WithValidCardAndDeal_ReturnsTrue()
        {
            // Arrange
            Guid offeredCardId = new Guid("your-card-id");
            var tradingDeal = new TradingDeal
            {
                Id = Guid.NewGuid(),
                MinimumDamage = 50,
                Type = "Monster"
                // Other properties as required
            };

            // Act
            bool result = _transactionRepository.DoesCardMeetTradeRequirements(offeredCardId, tradingDeal);

            // Assert
            Assert.IsTrue(result);
        }
    }
    
}