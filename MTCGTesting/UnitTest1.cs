using NUnit.Framework;
using MTCG.Database.Repository;
using System;
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
            var userId = 8; // Use a test user ID that exists in your database
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
        public void GetUserDeck_RetrievesUserDeck()
        {
            // Arrange
            // Assuming you have a known User ID in your database
            int userId = 7;

            // Act
            var deck = _cardRepository.GetUserDeck(userId);

            // Assert
            Assert.IsNotNull(deck);
            Assert.IsNotEmpty(deck);
        }
        
        [Test]
        public void GetUserDeck_RetrievesUsersDeck()
        {
            // Arrange
            int testUserId = 7; // Assuming this user ID exists in your test database
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
    
}