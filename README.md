Project Protocol: Multiplayer/Monster Trading Card Game (MTCG) Server
Overview

This protocol documents the development process, insights, and lessons learned during the creation of a Multiplayer Trading Card Game (MTCG) Server. The project involved designing and implementing a server to handle card battles, user management, trading, and other functionalities for an online trading card game.
Design Insights

    Architecture Design: The server architecture embraced a modular framework, emphasizing clear separation of functionalities including user management, battle mechanics, database operations, and HTTP request processing. This structure facilitated maintenance and scalability, allowing for focused enhancements and bug fixes within isolated modules.

    Database Schema Optimization: Leveraged PostgreSQL for its robust features, meticulously designing the database schema to ensure data normalization and efficiency. Relationships and constraints were strategically implemented to support complex game dynamics while optimizing query performance.

    Comprehensive Unit Testing: Implemented thorough unit tests across critical modules like user and card repositories, and battle logic, ensuring reliability and stability of each component independently.

    Concurrency Management: Developed sophisticated mechanisms to handle concurrent interactions, enabling multiple users to engage seamlessly with the system. For example, the ability to execute a battle in one thread while querying player statistics in another without conflict, demonstrated the system's robust handling of concurrent operations.

Lessons Learned

    Database Relationships: The project highlighted the critical role of foreign key constraints in maintaining data integrity and relationships, 
	such as linking users to their cards or trades. It was a lesson in designing schemas that support not just data storage but also the game's logic and rules.

    Concurrency Handling: Implementing the battle lobby brought forward the challenge of managing simultaneous user requests without data conflicts. 
	Techniques like locking and thread-safe operations were essential to ensure a smooth and fair game experience for all players.
	Though i dont really know if i did it correctly.

    Error Handling: Building a networked game server underscored the need for robust error handling to catch and respond to issues without crashing the server. 
	It was crucial to provide clear, actionable feedback to users, enhancing the game's usability and reliability.

    Unit Testing: The complexity of the game mechanics, from matchmaking to battle outcomes, necessitated thorough unit testing. 
	It ensured each component functioned correctly in isolation and when integrated, contributing to the game's overall stability and performance.

Unit Test Design

    Repository Testing: Focused on CRUD operations and ensuring data integrity in user and card repositories.
    Battle Logic Testing: Simulated various battle scenarios to test the game logic, including different card types, user interactions, and win/loss conditions.
    Integration Testing: Combined different components (like user authentication with battle logic) to ensure they work together as expected.
    Mocking: Used mocking for components like database access to isolate tests and focus on specific logic.
	
Unique Features
	Unique Battle Classes and Elements: I incorporated distinct classes and elements into the game, each governed by specific rules within the battle logic. This added depth and strategy to the gameplay.
    Battlelog: A comprehensive battle log is generated and preserved in a text document, serving both as a historical record and a tool for post-game analysis.
    Elo System Enhancement: Inspired by the robust Elo rating system used in competitive chess, I refined the game's ranking mechanics to more accurately reflect player skill.
    Win-Lose Ratio: Implemented a percentage-based system to provide a clearer representation of a player's performance.
    Draw Reduction Logic: Implemented algorithms aimed at minimizing the occurrence of draws, ensuring more decisive outcomes and a satisfying competitive experience.

Time Spent

    Total Hours: Approximately 65-70 hours.
    Breakdown:
		Aquiring the needed Skills : 10 hours
		Research : 2-4 hours
        Design/Plan/Creation of Database and Logic : 10-15 hours
        Database Implementation: 15 hours
        Server Logic Development: 20 hours
        Unit Testing and Debugging: 5-10 hours
        Documentation and Finalization: 2-3 hours

GitHub Repository

    https://github.com/Naxnor/MTCG_SWEN_RAFAEL_JEITLER
