CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "vector";
-- File: Sequences/SequenceAnalyzerRules.sql
CREATE SEQUENCE IF NOT EXISTS public."SequenceAnalyzerRules"
    INCREMENT BY 1
    START WITH 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

-- File: Sequences/SequenceArticleBugs.sql
CREATE SEQUENCE IF NOT EXISTS public."SequenceArticleBugs"
    INCREMENT BY 1
    START WITH 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

-- File: Sequences/SequenceArticles.sql
CREATE SEQUENCE IF NOT EXISTS public."SequenceArticles"
    INCREMENT BY 1
    START WITH 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

-- File: Sequences/SequenceDomainCollectionGroups.sql
CREATE SEQUENCE IF NOT EXISTS public."SequenceDomainCollectionGroups"
    INCREMENT BY 1
    START WITH 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

-- File: Sequences/SequenceDomainCollections.sql
CREATE SEQUENCE IF NOT EXISTS public."SequenceDomainCollections"
    INCREMENT BY 1
    START WITH 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

-- File: Sequences/SequenceDomains.sql
CREATE SEQUENCE IF NOT EXISTS public."SequenceDomains"
    INCREMENT BY 1
    START WITH 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

-- File: Sequences/SequenceDomainTypeMatches.sql
CREATE SEQUENCE IF NOT EXISTS public."SequenceDomainTypeMatches"
    INCREMENT BY 1
    START WITH 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

-- File: Sequences/SequenceDownloadQueue.sql
CREATE SEQUENCE IF NOT EXISTS public."SequenceDownloadQueue"
    INCREMENT BY 1
    START WITH 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

-- File: Sequences/SequenceDownloadRules.sql
CREATE SEQUENCE IF NOT EXISTS public."SequenceDownloadRules"
    INCREMENT BY 1
    START WITH 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

-- File: Sequences/SequenceFeedCategories.sql
CREATE SEQUENCE IF NOT EXISTS public."SequenceFeedCategories"
    INCREMENT BY 1
    START WITH 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

-- File: Sequences/SequenceFeeds.sql
CREATE SEQUENCE IF NOT EXISTS public."SequenceFeeds"
    INCREMENT BY 1
    START WITH 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

-- File: Sequences/SequenceJournalCategories.sql
CREATE SEQUENCE IF NOT EXISTS public."SequenceJournalCategories"
    INCREMENT BY 1
    START WITH 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

-- File: Sequences/SequenceJournalCheckListItems.sql
CREATE SEQUENCE IF NOT EXISTS public."SequenceJournalCheckListItems"
    INCREMENT BY 1
    START WITH 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

-- File: Sequences/SequenceJournalCheckLists.sql
CREATE SEQUENCE IF NOT EXISTS public."SequenceJournalCheckLists"
    INCREMENT BY 1
    START WITH 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

-- File: Sequences/SequenceJournalEntrySnapshots.sql
CREATE SEQUENCE IF NOT EXISTS public."SequenceJournalEntrySnapshots"
    INCREMENT BY 1
    START WITH 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

-- File: Sequences/SequenceJournalFiles.sql
CREATE SEQUENCE IF NOT EXISTS public."SequenceJournalFiles"
    INCREMENT BY 1
    START WITH 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

-- File: Sequences/SequenceJournalImages.sql
CREATE SEQUENCE IF NOT EXISTS public."SequenceJournalImages"
    INCREMENT BY 1
    START WITH 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

-- File: Sequences/SequenceJournals.sql
CREATE SEQUENCE IF NOT EXISTS public."SequenceJournals"
    INCREMENT BY 1
    START WITH 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

-- File: Sequences/SequenceJournalTags.sql
CREATE SEQUENCE IF NOT EXISTS public."SequenceJournalTags"
    INCREMENT BY 1
    START WITH 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

-- File: Sequences/SequenceJournalVideos.sql
CREATE SEQUENCE IF NOT EXISTS public."SequenceJournalVideos"
    INCREMENT BY 1
    START WITH 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

-- File: Sequences/SequenceStatisticsProjects.sql
CREATE SEQUENCE IF NOT EXISTS public."SequenceStatisticsProjects"
    INCREMENT BY 1
    START WITH 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

-- File: Sequences/SequenceStatisticsResults.sql
CREATE SEQUENCE IF NOT EXISTS public."SequenceStatisticsResults"
    INCREMENT BY 1
    START WITH 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

-- File: Sequences/SequenceSubjects.sql
CREATE SEQUENCE IF NOT EXISTS public."SequenceSubjects"
    INCREMENT BY 1
    START WITH 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

-- File: Sequences/SequenceWords.sql
CREATE SEQUENCE IF NOT EXISTS public."SequenceWords"
    INCREMENT BY 1
    START WITH 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

-- File: Tables/Articles/ArticleBugs.sql
CREATE TABLE IF NOT EXISTS public."ArticleBugs"
(
    "bugId" INT NOT NULL PRIMARY KEY,
    "articleId" INT NULL,
    "title" VARCHAR(100) NULL,
    "description" TEXT NULL,
    "datecreated" TIMESTAMP NULL,
    "status" SMALLINT NULL
);

-- File: Tables/Articles/ArticleDates.sql
CREATE TABLE IF NOT EXISTS public."ArticleDates"
(
    "articleId" INT NOT NULL PRIMARY KEY,
    "date" DATE NULL,
    "hasyear" BOOLEAN NULL,
    "hasmonth" BOOLEAN NULL,
    "hasday" BOOLEAN NULL
);

-- File: Tables/Articles/Articles.sql
CREATE TABLE IF NOT EXISTS public."Articles"
(
    "articleId" INT NOT NULL PRIMARY KEY,
    "feedId" INT NULL DEFAULT 0,
    "subjects" SMALLINT NULL DEFAULT 0,
    "subjectId" INT NULL DEFAULT 0,
    "domainId" INT NULL DEFAULT 0,
    "score" SMALLINT NULL DEFAULT 0,
    "images" SMALLINT NULL DEFAULT 0,
    "filesize" DOUBLE PRECISION NULL DEFAULT 0,
    "linkcount" INT DEFAULT 0,
    "linkwordcount" INT DEFAULT 0,
    "wordcount" INT DEFAULT 0,
    "sentencecount" SMALLINT DEFAULT 0,
    "paragraphcount" SMALLINT DEFAULT 0,
    "importantcount" SMALLINT DEFAULT 0,
    "analyzecount" SMALLINT DEFAULT 0,
    "yearstart" SMALLINT NULL DEFAULT 0,
    "yearend" SMALLINT NULL DEFAULT 0,
    "years" VARCHAR(50) DEFAULT '',
    "datecreated" TIMESTAMP NULL,
    "datepublished" TIMESTAMP NULL,
    "relavance" SMALLINT NULL DEFAULT 0,
    "importance" SMALLINT NULL DEFAULT 0,
    "fiction" SMALLINT NULL DEFAULT 1,
    "domain" VARCHAR(50) NULL DEFAULT '',
    "url" VARCHAR(250) NULL DEFAULT '',
    "title" VARCHAR(250) NULL DEFAULT '',
    "summary" VARCHAR(250) NULL DEFAULT '',
    "analyzed" DOUBLE PRECISION DEFAULT 0,
    "visited" INT NOT NULL DEFAULT 0,
    "cached" BOOLEAN NULL DEFAULT FALSE,
    "active" BOOLEAN NULL DEFAULT FALSE,
    "deleted" BOOLEAN NULL DEFAULT FALSE
);

-- File: Tables/Articles/ArticleSentences.sql
CREATE TABLE IF NOT EXISTS public."ArticleSentences"
(
    "articleId" INT NOT NULL,
    "index" SMALLINT NULL,
    "sentence" TEXT NULL
);

-- File: Tables/Articles/ArticleSubjects.sql
CREATE TABLE IF NOT EXISTS public."ArticleSubjects"
(
    "subjectId" INT NOT NULL,
    "articleId" INT NULL,
    "score" SMALLINT NULL,
    "datecreated" TIMESTAMP NULL,
    "datepublished" TIMESTAMP NULL
);

-- File: Tables/Articles/ArticleWords.sql
CREATE TABLE IF NOT EXISTS public."ArticleWords"
(
    "articleId" INT NOT NULL,
    "wordId" INT NOT NULL,
    "count" INT NULL
);

-- File: Tables/Blacklist_Domains.sql
CREATE TABLE IF NOT EXISTS public."Blacklist_Domains"
(
    "domain" VARCHAR(64) NOT NULL PRIMARY KEY
);

-- File: Tables/Blacklist_Wildcards.sql
CREATE TABLE IF NOT EXISTS public."Blacklist_Wildcards"
(
    "domain" VARCHAR(64) NOT NULL PRIMARY KEY
);

-- File: Tables/Users/AppUsers.sql
CREATE TABLE IF NOT EXISTS public."AppUsers"
(
    "Id" UUID PRIMARY KEY,
    "Email" VARCHAR(64) NOT NULL,
    "EmailConfirmed" BOOLEAN NOT NULL,
    "FullName" VARCHAR(64) NOT NULL DEFAULT '',
    "PasswordHash" TEXT NULL,
    "LockoutEndDate" TIMESTAMP NULL,
    "LockoutEnabled" BOOLEAN NOT NULL,
    "AccessFailedCount" INT NOT NULL DEFAULT 0,
    "AccessFailedTime" TIMESTAMPTZ NULL,
    "PasswordResetHash" VARCHAR(128) NULL,
    "PasswordResetTime" TIMESTAMPTZ NULL,
    "NewEmail" VARCHAR(64) NULL,
    "OneTimeLoginToken" VARCHAR(128) NULL,
    "OneTimeLoginExpiry" TIMESTAMP NULL,
    "EncryptionKey" VARCHAR(255) NULL,
    "EncryptionType" VARCHAR(16) NULL,
    "Status" INT NOT NULL DEFAULT 0,
    "Created" TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
);

-- File: Tables/Chat/Chats.sql
CREATE TABLE IF NOT EXISTS public."Chats"
(
    "Id" UUID NOT NULL PRIMARY KEY DEFAULT uuid_generate_v4(),
    "AppUserId" UUID NOT NULL,
    "Title" VARCHAR(128) NOT NULL,
    "Created" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Modified" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Status" INT NOT NULL DEFAULT 1 --0=deleted, 1=active
);
ALTER TABLE public."Chats"
    ADD CONSTRAINT "FK_Chats_AppUsers" FOREIGN KEY ("AppUserId") REFERENCES public."AppUsers"("Id");

-- File: Tables/Chat/ChatContextChunks.sql
CREATE TABLE IF NOT EXISTS public."ChatContextChunks"
(
    "Id" UUID NOT NULL PRIMARY KEY DEFAULT uuid_generate_v4(),
    "AppUserId" UUID NOT NULL,
    "ChatId" UUID NOT NULL,
    "Content" TEXT NOT NULL,
    "Embedding" VECTOR(768) NOT NULL,
    "Metadata" TEXT NULL,
    "Created" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);
ALTER TABLE public."ChatContextChunks"
    ADD CONSTRAINT "FK_ChatContextChunks_AppUsers" FOREIGN KEY ("AppUserId") REFERENCES public."AppUsers"("Id");
ALTER TABLE public."ChatContextChunks"
    ADD CONSTRAINT "FK_ChatContextChunks_Chats" FOREIGN KEY ("ChatId") REFERENCES public."Chats"("Id");

-- File: Tables/Chat/ChatHistory.sql
CREATE TABLE IF NOT EXISTS public."ChatHistory"
(
    "Id" UUID NOT NULL PRIMARY KEY DEFAULT uuid_generate_v4(),
    "ChatId" UUID NOT NULL,
    "Role" INT NOT NULL, --user, assistant, system
    "Content" TEXT NOT NULL,
    "Model" VARCHAR(50) NULL, --LLM model used (e.g., gpt-4, claude-3-opus, etc.)
    "Created" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Status" INT NOT NULL DEFAULT 1 --0=deleted, 1=active
);
ALTER TABLE public."ChatHistory"
    ADD CONSTRAINT "FK_ChatHistory_Chats" FOREIGN KEY ("ChatId") REFERENCES public."Chats"("Id");

-- File: Tables/Converse.sql
CREATE TABLE IF NOT EXISTS public."Converse"
(
    "Id" INT NOT NULL PRIMARY KEY,
    "AppUserId" UUID NOT NULL,
    "TEXT" TEXT NOT NULL,
    "Who" BOOLEAN NOT NULL,
    "Created" TIMESTAMP(6) DEFAULT CURRENT_TIMESTAMP
);
ALTER TABLE public."Converse"
    ADD CONSTRAINT "AK_Converse_User_Date" UNIQUE ("AppUserId", "Created");

-- File: Tables/ConverseResources.sql
CREATE TABLE IF NOT EXISTS public."ConverseResources"
(
    "ConverseId" INT NOT NULL PRIMARY KEY,
    "AppUserId" UUID NOT NULL,
    "SubjectId" INT NOT NULL
);
ALTER TABLE public."ConverseResources"
    ADD CONSTRAINT "AK_ConverseResources_User" UNIQUE ("AppUserId", "ConverseId");

-- File: Tables/ConverseSubjects.sql
CREATE TABLE IF NOT EXISTS public."ConverseSubjects"
(
    "ConverseId" INT NOT NULL PRIMARY KEY,
    "AppUserId" UUID NOT NULL,
    "SubjectId" INT NOT NULL
);
ALTER TABLE public."ConverseSubjects"
    ADD CONSTRAINT "AK_ConverseSubjects_User_Subject" UNIQUE ("AppUserId", "SubjectId", "ConverseId");

-- File: Tables/Dictionary.sql
CREATE TABLE IF NOT EXISTS public."Dictionary"
(
    "word" VARCHAR(25) NOT NULL PRIMARY KEY,
    "vocabtype" SMALLINT NULL,
    "grammertype" SMALLINT NULL,
    "socialtype" SMALLINT NULL,
    "objecttype" SMALLINT NULL,
    "score" SMALLINT NULL
);

-- File: Tables/Domains/AnalyzerRules.sql
CREATE TABLE IF NOT EXISTS public."AnalyzerRules"
(
    "ruleId" INT NOT NULL PRIMARY KEY,
    "domainId" INT NOT NULL,
    "selector" VARCHAR(64) NOT NULL DEFAULT '',
    "rule" BOOLEAN NOT NULL DEFAULT FALSE, -- 0 = exclude, 1 = include
    "datecreated" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- File: Tables/Domains/DomainAddresses.sql
CREATE TABLE IF NOT EXISTS public."DomainAddresses"
(
    "addressId" INT NOT NULL PRIMARY KEY,
    "address" VARCHAR(64) NOT NULL DEFAULT '',
    "city" VARCHAR(32) NOT NULL DEFAULT '',
    "state" VARCHAR(3) NOT NULL DEFAULT '',
    "zipcode" VARCHAR(12) NOT NULL DEFAULT ''
);

-- File: Tables/Domains/DomainCollectionGroups.sql
CREATE TABLE IF NOT EXISTS public."DomainCollectionGroups"
(
    "colgroupId" INT NOT NULL PRIMARY KEY,
    "name" VARCHAR(32) NOT NULL
);CREATE INDEX IF NOT EXISTS "IX_Domains_DomainCollectionGroupNames" ON public."DomainCollectionGroups" ("name" DESC);

-- File: Tables/Domains/DomainCollections.sql
CREATE TABLE IF NOT EXISTS public."DomainCollections"
(
    "colId" INT NOT NULL PRIMARY KEY,
    "colgroupId" INT NULL, -- collection group ID
    "name" VARCHAR(32) NOT NULL,
    "search" VARCHAR(128) NOT NULL DEFAULT '',
    "subjectId" INT NOT NULL DEFAULT 0,
    "filtertype" INT NOT NULL DEFAULT 0,
    "type" INT NOT NULL DEFAULT -1,
    "sort" INT NOT NULL DEFAULT 0,
    "lang" VARCHAR(6) NOT NULL DEFAULT '',
    "datecreated" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);CREATE INDEX IF NOT EXISTS "IX_Domains_DomainCollectionDates" ON public."DomainCollections" ("datecreated" DESC);

CREATE INDEX IF NOT EXISTS "IX_Domains_DomainCollectionNames" ON public."DomainCollections" ("name" DESC);

-- File: Tables/Domains/DomainHierarchy.sql
CREATE TABLE IF NOT EXISTS public."DomainHierarchy"
(
    "domainId" INT NOT NULL,
    "parentId" INT NOT NULL,
    "level" INT NOT NULL
);
ALTER TABLE public."DomainHierarchy"
    ADD CONSTRAINT PK_DomainHierarchy PRIMARY KEY ("domainId", "parentId");

-- File: Tables/Domains/DomainLinks.sql
CREATE TABLE IF NOT EXISTS public."DomainLinks"
(
    "domainId" INT NOT NULL,
    "linkId" INT NOT NULL
);
ALTER TABLE public."DomainLinks"
    ADD CONSTRAINT PK_DomainLinks PRIMARY KEY ("domainId", "linkId");

-- File: Tables/Domains/Domains.sql
CREATE TABLE IF NOT EXISTS public."Domains"
(
    "domainId" INT NOT NULL PRIMARY KEY,
    "domain" VARCHAR(64) NOT NULL,
    "lang" VARCHAR(6) NOT NULL DEFAULT 'en',
    "parentId" INT NOT NULL DEFAULT 0,
    "hastitle" BOOLEAN NOT NULL DEFAULT FALSE,
    "paywall" BOOLEAN NOT NULL DEFAULT FALSE,
    "free" BOOLEAN NOT NULL DEFAULT FALSE,
    "https" BOOLEAN NOT NULL DEFAULT FALSE,
    "www" BOOLEAN NOT NULL DEFAULT FALSE,
    "empty" BOOLEAN NOT NULL DEFAULT FALSE,
    "deleted" BOOLEAN NOT NULL DEFAULT FALSE,
    "type" INT NOT NULL DEFAULT -1,
    "type2" INT NOT NULL DEFAULT -1,
    "articles" INT NOT NULL DEFAULT 0,
    "inqueue" INT NOT NULL DEFAULT 0,
    "title" VARCHAR(128) NOT NULL DEFAULT '',
    "company" VARCHAR(64) NOT NULL DEFAULT '',
    "description" VARCHAR(255) NOT NULL DEFAULT '',
    "datecreated" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "dateupdated" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "lastchecked" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP -- last scraped a URL from the domain
);

-- File: Tables/Domains/DomainServiceNames.sql
CREATE TABLE IF NOT EXISTS public."DomainServiceNames"
(
    "Id" INT NOT NULL PRIMARY KEY GENERATED BY DEFAULT AS IDENTITY,
    "Name" VARCHAR(64) NOT NULL
);

-- File: Tables/Domains/DomainServices.sql
CREATE TABLE IF NOT EXISTS public."DomainServices"
(
    "domainId" INT NOT NULL,
    "serviceId" INT NOT NULL
);

-- File: Tables/Domains/DomainTypeMatches.sql
CREATE TABLE IF NOT EXISTS public."DomainTypeMatches"
(
    "matchId" INT NOT NULL PRIMARY KEY,
    "type" INT NOT NULL DEFAULT -1,
    "type2" INT NOT NULL DEFAULT -1,
    "words" TEXT, --json serialized object
    "threshold" INT NOT NULL DEFAULT 1, -- minimum number of matches that must be found to succeed
    "rank" INT NOT NULL DEFAULT 0 -- when there are multiple matches, choose the lowest rank first
);

-- File: Tables/Domains/DownloadRules.sql
CREATE TABLE IF NOT EXISTS public."DownloadRules"
(
	"ruleId" INT NOT NULL PRIMARY KEY, 
	"domainId" INT NOT NULL, 
    "rule" BOOLEAN NOT NULL DEFAULT FALSE, -- 0 = don't bother downloading at all, 1 = scrape links only and is not an article
    "url" VARCHAR(64) NOT NULL DEFAULT '',
    "title" VARCHAR(64) NOT NULL DEFAULT '',
    "summary" VARCHAR(64) NOT NULL DEFAULT '',
    "datecreated" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- File: Tables/Downloads/DownloadQueue.sql
CREATE TABLE IF NOT EXISTS public."DownloadQueue"
(
    "qid" BIGINT NOT NULL,
    "feedId" INT NULL,
    "domainId" INT NULL,
    "type" SMALLINT NULL,
    "status" INT NOT NULL DEFAULT 0,
    "tries" INT NOT NULL DEFAULT 0,
    "url" VARCHAR(255) NOT NULL,
    "path" VARCHAR(255) NOT NULL,
    "datecreated" TIMESTAMP(6) NOT NULL DEFAULT CURRENT_TIMESTAMP
);
ALTER TABLE public."DownloadQueue"
    ADD CONSTRAINT "PK_DownloadQueue" PRIMARY KEY ("qid");

-- File: Tables/Downloads/Downloads.sql
CREATE TABLE IF NOT EXISTS public."Downloads"
(
    "id" BIGINT NOT NULL,
    "feedId" INT NULL,
    "domainId" INT NULL,
    "type" SMALLINT NULL,
    "status" INT NOT NULL DEFAULT 0,
    "tries" INT NOT NULL DEFAULT 0,
    "url" VARCHAR(255) NOT NULL,
    "path" VARCHAR(255) NOT NULL,
    "datecreated" TIMESTAMP(6) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "datearchived" TIMESTAMP(6) NOT NULL DEFAULT CURRENT_TIMESTAMP
);
ALTER TABLE public."Downloads"
    ADD CONSTRAINT "PK_Downloads" PRIMARY KEY ("id");

-- File: Tables/Feeds/FeedCategories.sql
CREATE TABLE IF NOT EXISTS public."FeedCategories"
(
    "categoryId" INT NOT NULL PRIMARY KEY,
    "title" VARCHAR(64) NOT NULL
);

-- File: Tables/Feeds/Feeds.sql
CREATE TABLE IF NOT EXISTS public."Feeds"
(
    "feedId" INT NOT NULL PRIMARY KEY,
    "domainId" INT NOT NULL DEFAULT 0,
    "doctype" INT NULL, -- 1 = RSS, 2 = HTML
    "categoryId" INT NULL,
    "title" VARCHAR(100) NULL,
    "url" VARCHAR(100) NULL,
    "checkIntervals" INT NULL,
    "lastChecked" TIMESTAMP NULL,
    "filter" TEXT NULL
);

-- File: Tables/Feeds/FeedsCheckedLog.sql
CREATE TABLE IF NOT EXISTS public."FeedsCheckedLog"
(
    "feedId" INT NOT NULL,
    "links" SMALLINT NULL,
    "datechecked" TIMESTAMP NULL
);

-- File: Tables/Journal/JournalCategories.sql
CREATE TABLE IF NOT EXISTS public."JournalCategories"
(
    "Id" INT NOT NULL PRIMARY KEY DEFAULT (nextval('public."SequenceJournalCategories"')),
    "AppUserId" UUID NOT NULL,
    "Title" VARCHAR(64) NOT NULL,
    "Created" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Status" INT NOT NULL DEFAULT 1,
    "Color" VARCHAR(16) NOT NULL --hex color (no #)
);

-- File: Tables/Journal/JournalChapters.sql
CREATE TABLE IF NOT EXISTS public."JournalChapters"
(
    "ChapterId" INT NOT NULL,
    "JournalId" INT NOT NULL,
    "Title" VARCHAR(128) NOT NULL,
    "Sort" INT NOT NULL DEFAULT 1,
    "Icon" INT NOT NULL DEFAULT 0,
    "Color" INT NOT NULL DEFAULT 0,
    "Description" VARCHAR(256) NULL
);

-- File: Tables/Journal/JournalCheckListItems.sql
CREATE TABLE IF NOT EXISTS public."JournalCheckListItems"
(
    "Id" INT NOT NULL PRIMARY KEY DEFAULT (nextval('public."SequenceJournalCheckListItems"')),
    "CheckListId" INT NOT NULL,
    "Checked" BOOLEAN NOT NULL DEFAULT FALSE,
    "Title" VARCHAR(255) NOT NULL,
    "Icon" INT NOT NULL DEFAULT 0,
    "Created" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Status" INT NOT NULL DEFAULT 1,
    "Sort" INT NOT NULL DEFAULT 1
);

-- File: Tables/Journal/JournalCheckLists.sql
CREATE TABLE IF NOT EXISTS public."JournalCheckLists"
(
    "Id" INT NOT NULL PRIMARY KEY DEFAULT (nextval('public."SequenceJournalCheckLists"')),
    "AppUserId" UUID NOT NULL,
    "EntryId" UUID NOT NULL,
    "ThemeId" INT NULL,
    "Title" VARCHAR(64) NOT NULL,
    "Description" VARCHAR(512) NULL,
    "Created" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Status" INT NOT NULL DEFAULT 1
);

-- File: Tables/Journal/JournalEntries.sql
CREATE TABLE IF NOT EXISTS public."JournalEntries"
(
    "Id" UUID NOT NULL PRIMARY KEY DEFAULT uuid_generate_v4(),
    "JournalId" INT NOT NULL,
    "ParentEntryId" UUID NULL,
    "ChapterId" INT NULL,
    "Title" VARCHAR(128) NOT NULL,
    "Description" VARCHAR(512) NOT NULL,
    "Url" VARCHAR(255) NULL,
    "Created" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Modified" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Status" INT NOT NULL DEFAULT 1, --0=deleted, 1=active, 2=published, 3=archived
    "Encrypted" BOOLEAN NOT NULL DEFAULT FALSE, --if encrypted, cannot be published
    "Thumbnail" VARCHAR(128) NULL, --image to use as thumbnail
    "ThumbnailModuleId" VARCHAR(64) NULL, --module id that the thumbnail came from
    "Favorite" BOOLEAN NOT NULL DEFAULT FALSE --if entry is favorited
);

-- File: Tables/Journal/JournalEntrySnapshots.sql
CREATE TABLE IF NOT EXISTS public."JournalEntrySnapshots"
(
    "Id" INT NOT NULL PRIMARY KEY DEFAULT (nextval('public."SequenceJournalEntrySnapshots"')),
    "EntryId" UUID NOT NULL,
    "JournalId" INT NOT NULL,
    "ChapterId" INT NULL,
    "Title" VARCHAR(128) NOT NULL,
    "Description" VARCHAR(512) NOT NULL,
    "Created" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Modified" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedSnapshot" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Status" INT NOT NULL DEFAULT 1, --0=deleted, 1=active, 2=published, 3=archived
    "Encrypted" BOOLEAN NOT NULL DEFAULT FALSE, --if encrypted, cannot be published
    "Thumbnail" VARCHAR(128) NULL --image to use as thumbnail
);

-- File: Tables/Journal/JournalEntryTags.sql
CREATE TABLE IF NOT EXISTS public."JournalEntryTags"
(
    "TagId" INT NOT NULL,
    "JournalEntryId" UUID NOT NULL
);
ALTER TABLE public."JournalEntryTags"
    ADD CONSTRAINT "PK_JournalEntryTags" PRIMARY KEY ("TagId", "JournalEntryId");CREATE INDEX IF NOT EXISTS "IX_JournalEntryTags_JournalEntryId" ON public."JournalEntryTags" ("JournalEntryId");

CREATE INDEX IF NOT EXISTS "IX_JournalEntryTags_JournalEntryAndTag" ON public."JournalEntryTags" ("JournalEntryId", "TagId");

-- File: Tables/Journal/JournalFiles.sql
CREATE TABLE IF NOT EXISTS public."JournalFiles"
(
    "Id" INT NOT NULL PRIMARY KEY DEFAULT (nextval('public."SequenceJournalFiles"')),
    "JournalId" INT NOT NULL,
    "JournalEntryId" UUID NOT NULL,
    "ModuleId" VARCHAR(16) NOT NULL,
    "Filename" VARCHAR(256) NOT NULL,
    "FileSize" BIGINT NOT NULL DEFAULT 0,
    "DateUploaded" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);CREATE INDEX IF NOT EXISTS "IX_JournalFiles_ModuleId" ON public."JournalFiles" ("ModuleId");

CREATE INDEX IF NOT EXISTS "IX_JournalFiles_EntryAndModuleId" ON public."JournalFiles" ("JournalEntryId", "ModuleId");

CREATE INDEX IF NOT EXISTS "IX_JournalFiles_JournalEntryAndModuleId" ON public."JournalFiles" ("JournalId", "JournalEntryId", "ModuleId");

-- File: Tables/Journal/JournalImages.sql
CREATE TABLE IF NOT EXISTS public."JournalImages"
(
    "Id" INT NOT NULL PRIMARY KEY DEFAULT (nextval('public."SequenceJournalImages"')),
    "JournalId" INT NOT NULL,
    "JournalEntryId" UUID NOT NULL,
    "ModuleId" VARCHAR(16) NOT NULL,
    "Filename" VARCHAR(64) NOT NULL,
    "Width" INT NOT NULL DEFAULT 1,
    "Height" INT NOT NULL DEFAULT 1
);CREATE INDEX IF NOT EXISTS "IX_JournalImages_ModuleId" ON public."JournalImages" ("ModuleId");

CREATE INDEX IF NOT EXISTS "IX_JournalImages_EntryAndModuleId" ON public."JournalImages" ("JournalEntryId", "ModuleId");

CREATE INDEX IF NOT EXISTS "IX_JournalImages_JournalEntryAndModuleId" ON public."JournalImages" ("JournalId", "JournalEntryId", "ModuleId");

-- File: Tables/Journal/Journals.sql
CREATE TABLE IF NOT EXISTS public."Journals"
(
    "Id" INT NOT NULL PRIMARY KEY DEFAULT (nextval('public."SequenceJournals"')),
    "AppUserId" UUID NOT NULL,
    "CategoryId" INT NOT NULL,
    "Title" VARCHAR(64) NOT NULL,
    "Created" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Status" INT NOT NULL DEFAULT 1, --1 = active, 0 = archived, 8 = custom modules
    "Color" VARCHAR(16) NOT NULL, --hex color (no #)
    "EntryId" UUID NULL --default entry for the journal
);

-- File: Tables/Journal/JournalTags.sql
CREATE TABLE IF NOT EXISTS public."JournalTags"
(
    "Id" INT NOT NULL PRIMARY KEY DEFAULT (nextval('public."SequenceJournalTags"')),
    "JournalId" INT NOT NULL,
    "Tag" VARCHAR(32) NOT NULL
);CREATE INDEX IF NOT EXISTS "IX_JournalTags_JournalId" ON public."JournalTags" ("JournalId");

CREATE INDEX IF NOT EXISTS "IX_JournalTags_JournalAndTag" ON public."JournalTags" ("JournalId", "Tag");

-- File: Tables/Journal/JournalVideos.sql
CREATE TABLE IF NOT EXISTS public."JournalVideos"
(
    "Id" INT NOT NULL PRIMARY KEY DEFAULT (nextval('public."SequenceJournalVideos"')),
    "JournalId" INT NOT NULL,
    "JournalEntryId" UUID NOT NULL,
    "ModuleId" VARCHAR(16) NOT NULL,
    "Filename" VARCHAR(64) NOT NULL,
    "OriginalFilename" VARCHAR(64) NULL DEFAULT '',
    "Url" VARCHAR(128) NULL DEFAULT '',
    "Downloaded" BOOLEAN NOT NULL DEFAULT FALSE,
    "Duration" INT NOT NULL DEFAULT 0,
    "Width" INT NOT NULL DEFAULT 1,
    "Height" INT NOT NULL DEFAULT 1,
    "Created" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Metadata" VARCHAR(128) NULL,
    "Title" VARCHAR(128) NULL DEFAULT '',
    "Description" VARCHAR(50) NULL,
    "FileSizeMb" DECIMAL(10, 2) NOT NULL DEFAULT 0
);CREATE INDEX IF NOT EXISTS "IX_JournalVideos_ModuleId" ON public."JournalVideos" ("ModuleId");

CREATE INDEX IF NOT EXISTS "IX_JournalVideos_EntryAndModuleId" ON public."JournalVideos" ("JournalEntryId", "ModuleId");

CREATE INDEX IF NOT EXISTS "IX_JournalVideos_JournalEntryAndModuleId" ON public."JournalVideos" ("JournalId", "JournalEntryId", "ModuleId");

-- File: Tables/Statistics/StatisticsProjectResearchers.sql
CREATE TABLE IF NOT EXISTS public."StatisticsProjectResearchers"
(
    "projectId" INT NOT NULL PRIMARY KEY,
    "researcherId" INT NULL
);

-- File: Tables/Statistics/StatisticsProjects.sql
CREATE TABLE IF NOT EXISTS public."StatisticsProjects"
(
    "projectId" INT NOT NULL PRIMARY KEY,
    "title" VARCHAR(100) NULL,
    "url" VARCHAR(100) NULL,
    "summary" VARCHAR(250) NULL,
    "publishdate" TIMESTAMP NULL
);

-- File: Tables/Statistics/StatisticsResearchers.sql
CREATE TABLE IF NOT EXISTS public."StatisticsResearchers"
(
    "researcherId" INT NOT NULL PRIMARY KEY,
    "name" VARCHAR(100) NULL,
    "credentials" TEXT NULL,
    "bday" DATE NULL
);

-- File: Tables/Statistics/StatisticsResults.sql
CREATE TABLE IF NOT EXISTS public."StatisticsResults"
(
    "statId" INT NOT NULL PRIMARY KEY,
    "projectId" INT NULL,
    "year" INT NULL,
    "month" INT NULL,
    "day" INT NULL,
    "test" DOUBLE PRECISION NULL,
    "result" DOUBLE PRECISION NULL,
    "country" VARCHAR(3) NULL,
    "city" VARCHAR(45) NULL,
    "state" VARCHAR(5) NULL,
    "topic" VARCHAR(50) NULL,
    "target" VARCHAR(50) NULL,
    "sentence" VARCHAR(250) NULL
);

-- File: Tables/Subjects.sql
CREATE TABLE IF NOT EXISTS public."Subjects"
(
    "subjectId" INT NOT NULL PRIMARY KEY,
    "parentId" INT NULL DEFAULT 0,
    "grammartype" INT NULL DEFAULT 0,
    "score" INT NULL DEFAULT 0,
    "haswords" BOOLEAN NULL DEFAULT FALSE,
    "title" VARCHAR(50) NULL DEFAULT '',
    "hierarchy" VARCHAR(50) NULL DEFAULT '',
    "breadcrumb" VARCHAR(500) NULL DEFAULT ''
);

-- File: Tables/Users/AppRoles.sql
CREATE TABLE IF NOT EXISTS public."AppRoles"
(
    "Id" INT NOT NULL PRIMARY KEY GENERATED BY DEFAULT AS IDENTITY,
    "Name" VARCHAR(256) NOT NULL
);

-- File: Tables/Users/AppUserRoles.sql
CREATE TABLE IF NOT EXISTS public."AppUserRoles"
(
    "AppUserId" UUID NOT NULL,
    "AppRoleId" INT NOT NULL,
    PRIMARY KEY ("AppUserId", "AppRoleId")
);
ALTER TABLE public."AppUserRoles"
    ADD CONSTRAINT FK_AppUserRoles_AppUserId FOREIGN KEY ("AppUserId") REFERENCES public."AppUsers"("Id");
ALTER TABLE public."AppUserRoles"
    ADD CONSTRAINT FK_AppUserRoles_AppRoleId FOREIGN KEY ("AppRoleId") REFERENCES public."AppRoles"("Id");

-- File: Tables/Users/AppUserTokens.sql
CREATE TABLE IF NOT EXISTS public."AppUserTokens"
(
    "Token" VARCHAR(255) NOT NULL PRIMARY KEY,
    "AppUserId" UUID NULL,
    "IsSpecialUser" BOOLEAN NOT NULL DEFAULT FALSE,
    "SpecialUserName" VARCHAR(32) NULL,
    "Expiry" TIMESTAMP NOT NULL,
    "Created" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "IPAddress" VARCHAR(50) NULL,
    "Revoked" TIMESTAMP NULL,
    "ReplacedByToken" VARCHAR(128) NULL
);
ALTER TABLE public."AppUserTokens"
    ADD CONSTRAINT "FK_AppUserTokens_AppUsers" FOREIGN KEY ("AppUserId") REFERENCES public."AppUsers"("Id");

-- File: Tables/Whitelist_Domains.sql
CREATE TABLE IF NOT EXISTS public."Whitelist_Domains"
(
    "domain" VARCHAR(64) NOT NULL PRIMARY KEY
);

-- File: Tables/Words/CommonWords.sql
CREATE TABLE IF NOT EXISTS public."CommonWords"
(
    "word" VARCHAR(16) NOT NULL PRIMARY KEY
);

-- File: Tables/Words/SubjectWords.sql
CREATE TABLE IF NOT EXISTS public."SubjectWords"
(
    "wordId" INT NOT NULL PRIMARY KEY,
    "subjectId" INT NOT NULL
);

-- File: Tables/Words/Words.sql
CREATE TABLE IF NOT EXISTS public."Words"
(
    "wordId" INT NOT NULL PRIMARY KEY,
    "word" VARCHAR(64) NOT NULL,
    "grammartype" INT NULL,
    "score" INT NULL
);

-- File: Indexes/IndexArticleBugs.sql
CREATE INDEX IF NOT EXISTS "IndexArticleBugs" ON public."ArticleBugs" ("articleId" ASC);

-- File: Indexes/IndexArticleDateCreated.sql
CREATE INDEX IF NOT EXISTS "IndexArticleDateCreated" ON public."Articles" ("datecreated" ASC);

CREATE INDEX IF NOT EXISTS "IndexArticleDateCreatedDesc" ON public."Articles" ("datecreated" DESC);

-- File: Indexes/IndexArticleDomain.sql
CREATE INDEX IF NOT EXISTS "IndexArticleDomain" ON public."Articles" ("domainId");

-- File: Indexes/IndexArticleScore.sql
CREATE INDEX IF NOT EXISTS "IndexArticleScore" ON public."Articles" ("score" ASC);

CREATE INDEX IF NOT EXISTS "IndexArticleScoreDesc" ON public."Articles" ("score" DESC);

-- File: Indexes/IndexArticleSentences.sql
CREATE INDEX IF NOT EXISTS "IndexArticleSentences" ON public."ArticleSentences" ("articleId" ASC);

-- File: Indexes/IndexArticleSubjects.sql
CREATE INDEX IF NOT EXISTS "IndexArticleSubjects" ON public."ArticleSubjects" ("subjectId" ASC, "datepublished" DESC, "datecreated" DESC);

-- File: Indexes/IndexArticleTitle.sql
CREATE INDEX IF NOT EXISTS "IndexArticleTitles" ON public."Articles" ("title" ASC);

CREATE INDEX IF NOT EXISTS "IndexArticleTitlesDesc" ON public."Articles" ("title" DESC);

-- File: Indexes/IndexArticleUrl.sql
CREATE INDEX IF NOT EXISTS "IndexArticleUrl" ON public."Articles" ("url" ASC);

CREATE INDEX IF NOT EXISTS "IndexArticleUrlDesc" ON public."Articles" ("url" DESC);

-- File: Indexes/IndexArticleVisited.sql
CREATE INDEX IF NOT EXISTS "IndexArticleVisited" ON public."Articles" ("visited" DESC);

-- File: Indexes/IndexArticleWords.sql
CREATE INDEX IF NOT EXISTS "IndexArticleWords" ON public."ArticleWords" ("wordId");

-- File: Indexes/IndexArticleWordsByArticle.sql
CREATE INDEX IF NOT EXISTS "IndexArticleWordsByArticle" ON public."ArticleWords" ("articleId");

-- File: Indexes/IndexDictionary.sql
CREATE INDEX IF NOT EXISTS "IndexDictionary" ON public."Dictionary" ("word" ASC);

-- File: Indexes/IndexDomainHierarchy.sql
CREATE INDEX IF NOT EXISTS "IX_DomainHierarchy_Domain" ON public."DomainHierarchy" ("domainId");

CREATE INDEX IF NOT EXISTS "IX_DomainHierarchy_Parent" ON public."DomainHierarchy" ("parentId", "level");

-- File: Indexes/IndexDomains.sql
CREATE INDEX IF NOT EXISTS "IndexDomainNames" ON public."Domains" ("domain");

CREATE INDEX IF NOT EXISTS "IndexDomainNamesDesc" ON public."Domains" ("domain" DESC);

CREATE INDEX IF NOT EXISTS "IndexDomainsCreated" ON public."Domains" ("datecreated");

CREATE INDEX IF NOT EXISTS "IndexDomainsLastChecked" ON public."Domains" ("lastchecked" DESC);

CREATE INDEX IF NOT EXISTS "IndexDomainsCreatedDesc" ON public."Domains" ("datecreated" DESC);

CREATE INDEX IF NOT EXISTS "IndexDomainArticles" ON public."Domains" ("articles" DESC);

CREATE INDEX IF NOT EXISTS "IX_Domains_Title" ON public."Domains" ("title");

CREATE INDEX IF NOT EXISTS "IX_Domains_HasTitle" ON public."Domains" ("hastitle" DESC);

CREATE INDEX IF NOT EXISTS "IX_Domains_Language" ON public."Domains" ("lang")
    INCLUDE ("domain", "paywall", "free");

-- File: Indexes/IndexDomainServices.sql
CREATE INDEX IF NOT EXISTS "IndexDomainServicesDomainId" ON public."DomainServices" ("domainId");

CREATE INDEX IF NOT EXISTS "IndexDomainServicesServiceId" ON public."DomainServices" ("serviceId", "domainId");

-- File: Indexes/IndexDownloadQueueDateCreated.sql
CREATE INDEX IF NOT EXISTS "IndexDownloadQueueDateCreatedDesc" ON public."DownloadQueue" ("datecreated" DESC);

CREATE INDEX IF NOT EXISTS "IndexDownloadQueueFeedUrlDateCreatedDesc" ON public."DownloadQueue" ("feedId", "url", "datecreated" DESC);

CREATE INDEX IF NOT EXISTS "IndexDownloadQueueDomainStatus" ON public."DownloadQueue" ("domainId", "status")
    INCLUDE ("feedId", "url", "datecreated");

-- File: Indexes/IndexDownloadQueueUrls.sql
CREATE INDEX IF NOT EXISTS "IndexDownloadQueueUrl" ON public."DownloadQueue" ("url");

-- File: Indexes/IndexDownloadUrls.sql
CREATE INDEX IF NOT EXISTS "IndexDownloadUrl" ON public."Downloads" ("url");

-- File: Indexes/IndexStatisticsResults.sql
CREATE INDEX IF NOT EXISTS "IndexStatisticsResults" ON public."StatisticsResults" ("projectId" ASC, "statId" ASC);

-- File: Indexes/IndexStatisticsResultsDate.sql
CREATE INDEX IF NOT EXISTS "IndexStatisticsResultsDate" ON public."StatisticsResults" ("year" ASC, "month" ASC, "day" ASC);

-- File: Indexes/IndexStatisticsResultsLocation.sql
CREATE INDEX IF NOT EXISTS "IndexStatisticsResultsLocation" ON public."StatisticsResults" ("country" ASC, "state" ASC, "city" ASC);

-- File: Indexes/IndexStatisticsResultsProject.sql
CREATE INDEX IF NOT EXISTS "IndexStatisticsResultsProject" ON public."StatisticsResults" ("projectId" ASC);

-- File: Indexes/IndexSubjectsBreadcrumb.sql
CREATE INDEX IF NOT EXISTS "IndexSubjectsBreadcrumb" ON public."Subjects" ("breadcrumb");

-- File: Indexes/IndexSubjectsHierarchy.sql
CREATE INDEX IF NOT EXISTS "IndexSubjectsHierarchy" ON public."Subjects" ("hierarchy");

-- File: Indexes/IndexWords.sql
CREATE INDEX IF NOT EXISTS "IndexWords" ON public."Words" ("word");
