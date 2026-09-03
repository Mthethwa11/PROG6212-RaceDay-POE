/* =========================================================
   DATA6222 Practical Assignment - Art Gallery Database
   Full T-SQL Script (Microsoft SQL Server syntax)
   ========================================================= */

/* ================= STEP 1: CREATE DATABASE & TABLES ================= */

CREATE DATABASE ArtGalleryDB;
GO

USE ArtGalleryDB;
GO

CREATE TABLE Genre (
    GenreID     INT IDENTITY(1,1) PRIMARY KEY,
    Description VARCHAR(100) NOT NULL
);

CREATE TABLE Artist (
    ArtistID INT IDENTITY(1,1) PRIMARY KEY,
    Name     VARCHAR(50) NOT NULL,
    Surname  VARCHAR(50) NOT NULL
);

CREATE TABLE Artwork (
    ArtworkID INT IDENTITY(1,1) PRIMARY KEY,
    GenreID   INT NOT NULL,
    ArtistID  INT NOT NULL,
    Title     VARCHAR(150) NOT NULL,
    CONSTRAINT FK_Artwork_Genre  FOREIGN KEY (GenreID)  REFERENCES Genre(GenreID),
    CONSTRAINT FK_Artwork_Artist FOREIGN KEY (ArtistID) REFERENCES Artist(ArtistID)
);

CREATE TABLE Exhibition (
    ExhibitionID INT IDENTITY(1,1) PRIMARY KEY,
    Description  VARCHAR(150) NOT NULL
);

CREATE TABLE Entry (
    EntryID      INT IDENTITY(1,1) PRIMARY KEY,
    ArtworkID    INT NOT NULL,
    ExhibitionID INT NOT NULL,
    CONSTRAINT FK_Entry_Artwork    FOREIGN KEY (ArtworkID)    REFERENCES Artwork(ArtworkID),
    CONSTRAINT FK_Entry_Exhibition FOREIGN KEY (ExhibitionID) REFERENCES Exhibition(ExhibitionID)
);
GO


/* ================= STEP 2: POPULATE THE DATABASE ================= */

-- Genre (5 records)
INSERT INTO Genre (Description) VALUES
('Abstract'), ('Impressionism'), ('Surrealism'), ('Portraiture'), ('Landscape');

-- Artist (6 records)
INSERT INTO Artist (Name, Surname) VALUES
('Thandiwe', 'Mokoena'), ('Liam', 'Bennett'), ('Aiko', 'Tanaka'),
('Carlos', 'Rivera'), ('Naledi', 'Dlamini'), ('Sofia', 'Rossi');

-- Artwork (22 records)
INSERT INTO Artwork (GenreID, ArtistID, Title) VALUES
(1, 1, 'Fractured Horizon'), (1, 2, 'Silent Geometry'), (1, 3, 'Chaos Theory'),
(2, 1, 'Morning at the Harbour'), (2, 4, 'Sunlit Fields'), (2, 5, 'Reflections on the Lake'),
(3, 3, 'Dreaming Machines'), (3, 6, 'The Melting Clock'), (3, 2, 'Whispers of the Subconscious'),
(4, 1, 'Portrait of a Stranger'), (4, 5, 'The Old Fisherman'), (4, 4, 'Woman in Blue'),
(5, 6, 'Valley at Dusk'), (5, 2, 'Mountains Beyond'), (5, 3, 'The Quiet River'),
(1, 4, 'Concrete Dreams'), (2, 6, 'Golden Wheatfield'), (3, 1, 'Floating City'),
(4, 3, 'Self Reflection'), (5, 5, 'Autumn Path'), (1, 5, 'Broken Symmetry'), (2, 3, 'Coastal Light');

-- Exhibition (16 records)
INSERT INTO Exhibition (Description) VALUES
('Spring Contemporary Showcase'), ('Durban Art Fair'), ('New Voices in Art'),
('The Colour of Emotion'), ('Modernist Perspectives'), ('Cape Town Biennale'),
('Emerging Artists Exhibit'), ('Reflections: A Group Show'), ('Abstract Visions'),
('Portraits of Our Time'), ('Nature Reimagined'), ('Surreal Worlds'),
('Winter Salon'), ('Coastal Impressions'), ('The Human Form'), ('Gallery 21 Annual Show');

-- Entry (25 records; ArtworkID 1, 4 and 21 each entered in more than 1 exhibition)
INSERT INTO Entry (ArtworkID, ExhibitionID) VALUES
(1, 1), (1, 9), (2, 1), (3, 9), (4, 2), (4, 14), (5, 2), (6, 14), (7, 12), (8, 12),
(9, 3), (10, 10), (11, 10), (12, 15), (13, 11), (14, 5), (15, 11), (16, 9), (17, 8),
(18, 12), (19, 10), (20, 11), (21, 16), (21, 6), (22, 8);
GO


/* ================= STEP 3: UPDATE STATEMENT ================= */

-- Demonstrates the UPDATE statement: correcting an artist's surname
SELECT * FROM Artist WHERE ArtistID = 1;   -- check the record before updating

UPDATE Artist
SET Surname = 'Mokoena-Ndlovu'
WHERE ArtistID = 1;

SELECT * FROM Artist WHERE ArtistID = 1;   -- confirm the update
GO


/* ================= STEP 4: DELETE STATEMENT ================= */

-- Precaution: check for dependent Artwork records before deleting a Genre,
-- since Genre is a parent table referenced by Artwork (FK_Artwork_Genre).
-- Deleting a genre that still has artworks linked to it would violate the
-- foreign key constraint and must be avoided.

INSERT INTO Genre (Description) VALUES ('Digital Art');   -- new genre, not yet linked to any artwork

SELECT COUNT(*) AS LinkedArtworks
FROM Artwork
WHERE GenreID = (SELECT GenreID FROM Genre WHERE Description = 'Digital Art');
-- Result = 0, so it is safe to delete this genre

DELETE FROM Genre
WHERE Description = 'Digital Art';

SELECT * FROM Genre;   -- confirm the deletion
GO


/* ================= STEP 5: ARTWORK / ARTIST / GENRE REPORT ================= */

-- Lists all artwork titles, their artists, and their genres.
-- Sorted alphabetically by genre, then alphabetically by artwork title.

SELECT
    g.Description                          AS Genre,
    a.Title                                 AS ArtworkTitle,
    CONCAT(ar.Name, ' ', ar.Surname)        AS Artist
FROM Artwork a
INNER JOIN Genre g  ON a.GenreID  = g.GenreID
INNER JOIN Artist ar ON a.ArtistID = ar.ArtistID
ORDER BY g.Description ASC, a.Title ASC;
GO


/* ================= STEP 6: GROUP BY REPORT ================= */

-- Purpose: shows how many artworks fall under each genre, giving the gallery
-- an overview of which genres are most represented in the collection.

SELECT
    g.Description         AS Genre,
    COUNT(a.ArtworkID)    AS NumberOfArtworks
FROM Artwork a
INNER JOIN Genre g ON a.GenreID = g.GenreID
GROUP BY g.Description
ORDER BY g.Description;
GO


/* ================= STEP 7: HAVING CLAUSE REPORT ================= */

-- Purpose: identifies the gallery's most prolific artists - those who have
-- created more than 3 artworks - to help decide who to feature in a
-- dedicated retrospective exhibition.

SELECT
    ar.Name,
    ar.Surname,
    COUNT(a.ArtworkID) AS NumberOfArtworks
FROM Artwork a
INNER JOIN Artist ar ON a.ArtistID = ar.ArtistID
GROUP BY ar.ArtistID, ar.Name, ar.Surname
HAVING COUNT(a.ArtworkID) > 3
ORDER BY NumberOfArtworks DESC;
GO


/* ================= STEP 8: JOIN REPORT ================= */

-- Purpose: produces an exhibition catalogue showing every artwork alongside
-- each exhibition it has been entered into, useful for tracking an
-- artwork's exhibition history.

SELECT
    a.Title             AS ArtworkTitle,
    e.Description        AS ExhibitionEntered
FROM Artwork a
INNER JOIN Entry en      ON a.ArtworkID = en.ArtworkID
INNER JOIN Exhibition e  ON en.ExhibitionID = e.ExhibitionID
ORDER BY a.Title;
GO
