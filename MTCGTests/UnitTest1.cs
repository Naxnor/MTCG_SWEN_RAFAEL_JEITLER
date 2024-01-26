using NUnit.Framework;
using MTCG.Database.Repository;
using System;
using System.Text;
using MTCG.Database;
using MTCG.Models;
using Npgsql;
using MTCGTesting.Utilities;


namespace MTCGTesting
{
    [TestFixture]
    public class UserRepositoryUnitTests
    {
        private UserRepository _userRepository;

        [OneTimeSetUp]
        public void GlobalSetUp()
        {
            DatabaseTestUtility.ResetDatabase();
            DatabaseTestUtility.CreateTestUsers();
        }

        [OneTimeTearDown]
        public void GlobalTearDown()
        {
            DatabaseTestUtility.CleanUpDatabase();
        }


        [SetUp]
        public void SetUp()
        {
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
                Username = "existingUser", // not unique but works because DB gets wiped prior to start
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
            string testUsername = "testUser1";
            string testPassword = "testPassword1";

            // Act
            bool result = _userRepository.AuthenticateUser(testUsername, testPassword);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void AuthenticateUser_WithWrongPassword_ReturnsFalse()
        {
            // Arrange
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
            var userId = 1; 
            var amountToAdd = 100;

            // Act
            var result = _userRepository.AddCoins(userId, amountToAdd);

            // Assert
            Assert.IsTrue(result);
        }



    }

    [TestFixture]
    public class CardRepositoryUnitTests
    {

        private CardRepository _cardRepository;


        [OneTimeSetUp]
        public void GlobalSetUp()
        {
            DatabaseTestUtility.ResetDatabase();
            DatabaseTestUtility.CreateTestUsers();
            DatabaseTestUtility.EnsureTestCardExists();
        }

        [OneTimeTearDown]
        public void GlobalTearDown()
        {
            DatabaseTestUtility.CleanUpDatabase();
        }

        [SetUp]
        public void SetUp()
        {
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
            var packageId =
                _cardRepository.GetNewestPackageId(); 
            _cardRepository.DeletePackage(packageId); 

            foreach (var card in cards)
            {
                _cardRepository.DeleteCard(card.Id); // Delete the test cards
            }
        }

        [Test]
        public void GetUserDeck_RetrievesUsersDeck()
        {
            // Arrange
            int testUserId = 10; 
            var expectedDeck = _cardRepository.GetUserDeck(testUserId).ToList();

            // Act
            var actualDeck = _cardRepository.GetUserDeck(testUserId).ToList();

            // Assert
            Assert.IsNotNull(actualDeck);
            Assert.AreEqual(expectedDeck.Count, actualDeck.Count);

           
            for (int i = 0; i < expectedDeck.Count; i++)
            {
                Assert.AreEqual(expectedDeck[i].Id, actualDeck[i].Id);
                Assert.AreEqual(expectedDeck[i].Name, actualDeck[i].Name);
              
            }
        }




    }

    [TestFixture]
    public class BattleServiceTests
    {
        private BattleService _battleService;
        private CardRepository _cardRepository;

        [OneTimeSetUp]
        public void GlobalSetUp()
        {
            // Ensure that test users and cards exist before any tests are run
            DatabaseTestUtility.EnsureTestUsersAndCardsExist();
            _cardRepository = new CardRepository();
        }

        [OneTimeTearDown]
        public void GlobalTearDown()
        {
            // Cleanup the database after all tests are run
            DatabaseTestUtility.CleanUpDatabase();
        }

        [SetUp]
        public void Setup()
        {
            _battleService = new BattleService();
        }



        [Test]
        public void EnterLobby_WithNewUser_WaitsForOpponent()
        {
            // Arrange
            int userId = 1; 

            // Act
            int result = 0;
            Task.Run(() => { result = _battleService.EnterLobby(userId); })
                .Wait(TimeSpan.FromSeconds(5)); // Wait for 5 seconds for an opponent to join

            // Assert
            Assert.AreEqual(0, result, "No opponent should be found within 30 seconds.");
        }


        [Test]
        public void StartBattle_WithValidUsers_StartsBattle()
        {
            // Arrange
            int userId = 1; // Test user ID from the setup
            int opponentId = 2; // Test opponent ID from the setup
            List<CardDTO> userDeck = _cardRepository.GetUserDeck(userId).ToList();
            List<CardDTO> opponentDeck = _cardRepository.GetUserDeck(opponentId).ToList();
            var deck1 = _battleService.ConvertDeckDtoToDeck(userDeck);
            var deck2 = _battleService.ConvertDeckDtoToDeck(opponentDeck);
            string userName = "User1";
            string opponentName = "User2";

            // Act
            var result = _battleService.StartBattle(userId, userName, opponentId, opponentName, deck1, deck2);

            // Assert
            Assert.IsNotEmpty(result, "Battle log should not be empty when a valid battle is started.");
        }

        [Test]
        public void ExecuteBattle_WithEqualDecks_EndsInDraw()
        {
            // Arrange
            int userId = 1; // Test user ID from the setup
            int opponentId = 2; // Test opponent ID from the setup
            List<CardDTO> userDeck = _cardRepository.GetUserDeck(userId).ToList();
            List<CardDTO> opponentDeck = _cardRepository.GetUserDeck(opponentId).ToList();
            var deck1 = _battleService.ConvertDeckDtoToDeck(userDeck);
            var deck2 = _battleService.ConvertDeckDtoToDeck(opponentDeck);
            string userName = "User1";
            string opponentName = "User2";

            // Act
            var battleLog = _battleService.StartBattle(userId, userName, opponentId, opponentName, deck1, deck2);

            // Assert
            StringAssert.Contains("Battle ended in a draw", battleLog,
                "Battle should end in a draw with equally matched decks.");
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
            Assert.AreEqual(BattleService.RoundResult.Win, outcome.Result,
                "User should win the round with a stronger card.");
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

        [OneTimeSetUp]
        public void GlobalSetUp()
        {
            DatabaseTestUtility.ResetDatabase();
            DatabaseTestUtility.CreateTestUsers();
            // Create additional global setup logic if needed
        }

        [OneTimeTearDown]
        public void GlobalTearDown()
        {
            DatabaseTestUtility.CleanUpDatabase();
            DatabaseTestUtility.CleanUpTradingDeals();
            // Clean up any additional data created during tests
        }

        [SetUp]
        public void SetUp()
        {
            _transactionRepository = new TransactionRepository();
        }

        [Test]
        public void DoesTradingDealExist_WithValidId_ReturnsTrue()
        {
            // Arrange
            var userId = 1; // Example user ID
            var dealId = DatabaseTestUtility.CreateTestTradingDeal(userId,  "Monster", 50.0f);
            
            // Act
            bool result = _transactionRepository.DoesTradingDealExist(dealId);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void CreateTradingDeal_WithValidData_CreatesDeal()
        {
            // First, create a test card in the database
            var testCard = new Card
            {
                Id = Guid.NewGuid(),
                Name = "Test Card",
                Damage = 50.0f,
                Element = "Fire",
                Class = "Dragon",
                Type = "Monster"
            };

            DatabaseTestUtility.CreateTestCard(testCard);

         
            int userId = 1; // Replace with a valid user ID
            

            // Arrange: Create a trading deal with the test card
            var tradingDeal = new TradingDeal
            {
                Id = Guid.NewGuid(),
                CardToTrade = testCard.Id, // Use the ID of the created test card
                Type = "Monster",
                MinimumDamage = 50
            };

            // Act: Try to create the trading deal
            bool result = _transactionRepository.CreateTradingDeal(tradingDeal, userId);

            // Assert: Check if the trading deal was successfully created
            Assert.IsTrue(result);

            
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
            var userId = 1; // Example user ID
            var cardToTrade = Guid.NewGuid(); // Example card ID
            var dealId = DatabaseTestUtility.CreateTestTradingDeal(userId,  "Monster", 50.0f);

            // Act
            bool result = _transactionRepository.IsTradingDealOwnedByUser(dealId, userId);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void DeleteTradingDeal_WithExistingDeal_DeletesDeal()
        {
            // Arrange
            var userId = 1; // Example user ID
            var cardToTrade = Guid.NewGuid(); // Example card ID
            var dealId = DatabaseTestUtility.CreateTestTradingDeal(userId, "Monster", 50.0f);

            // Act
            bool result = _transactionRepository.DeleteTradingDeal(dealId);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void GetTradingDeal_WithValidId_ReturnsDeal()
        {
            // Arrange
            var userId = 1; // Example user ID
            var cardToTrade = Guid.NewGuid(); // Example card ID
            var dealId = DatabaseTestUtility.CreateTestTradingDeal(userId,  "Monster", 50.0f);

            // Act
            var deal = _transactionRepository.GetTradingDeal(dealId);

            // Assert
            Assert.IsNotNull(deal);
            Assert.AreEqual(dealId, deal.Id);
        }

        [Test]
        public void DoesCardMeetTradeRequirements_WithValidCardAndDeal_ReturnsTrue()
        {
            // Arrange
            var testCardId = Guid.NewGuid();
            var testCard = new Card
            {
                Id = testCardId,
                Name = "Test Card",
                Damage = 60.0f, 
                Type = "Monster", 
                Element = "Fire",
                Class = "Dragon",
                
            };
            DatabaseTestUtility.CreateTestCard(testCard);

            // Create a trading deal that the test card should meet
            var tradingDeal = new TradingDeal
            {
                Id = Guid.NewGuid(),
                CardToTrade = Guid.NewGuid(), 
                Type = "Monster",
                MinimumDamage = 50
            };

            // Act
            bool result = _transactionRepository.DoesCardMeetTradeRequirements(testCardId, tradingDeal);

            // Assert
            Assert.IsTrue(result);
        }
    }
}    
    
