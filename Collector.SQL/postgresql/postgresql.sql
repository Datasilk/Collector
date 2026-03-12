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

-- File: Functions/GetPathFromUrl.sql
CREATE OR REPLACE FUNCTION public."GetPathFromUrl"
(
	url TEXT,
	domain VARCHAR(255)
);
RETURNS TEXT
AS
BEGIN
	-- get path by removing all text to the left of the domain name (and removing the domain name too)
	DECLARE result TEXT = REPLACE(REPLACE(SUBSTRING(url, CHARINDEX(domain, url) + LEN(domain) + 1, LEN(url) - CHARINDEX(domain, url) + LEN(domain)), 'index.html', ''), 'index.php', '')
	
	--remove query string
	IF CHARINDEX('?', result, 1) > 1 BEGIN
		SET result = SUBSTRING(result, 1, CHARINDEX('?', result, 1) - 1)
	END
	
	-- next, remove lingering slashes
	IF LEN(result) > =1 AND LEFT(result, 1) = '/' SET result = RIGHT(result, LEN(result) - 1)--first slash
	IF LEN(result) > 1 AND RIGHT(result, 1) = '/' SET result = LEFT(result, LEN(result) - 1) --last slash

	-- next, check if file extension exists
	DECLARE reverse TEXT = REVERSE(result)
	DECLARE slash INT = CHARINDEX('/', reverse)
	IF(SUBSTRING(result, LEN(result) - 3, 1) = '.' OR SUBSTRING(result, LEN(result) - 4, 1) = '.') BEGIN
		-- next, reverse the result and find the last slash
		IF slash >= 1 BEGIN
			SET result = REVERSE(SUBSTRING(reverse, slash + 1, LEN(reverse) - slash))
		END ELSE BEGIN
			SET result = ''
		END
	END
	
	-- next, remove the last item in the path
	--SET reverse = REVERSE(result)
	--SET slash = CHARINDEX('/', reverse)
	--IF slash >= 4 BEGIN
	--	SET result = REVERSE(SUBSTRING(reverse, slash + 1, LEN(reverse) - slash))
	--END ELSE BEGIN
	--	SET result = ''
	--END

	-- finally, check if last item in path is too long to be part of path
	WHILE 1 = 1 BEGIN
		IF LEN(result) <= 1 BEGIN
			SET result = ''
			BREAK
		END
		SET reverse = REVERSE(result)
		SET slash = CHARINDEX('/', reverse)
		IF slash >= 20 BEGIN
			SET result = REVERSE(SUBSTRING(reverse, slash + 1, LEN(reverse) - slash))
		END ELSE BEGIN
			BREAK
		END
	END
	RETURN result
END

-- File: Functions/SplitArray.sql
CREATE OR REPLACE FUNCTION SplitArray 
 (
 text  varchar(8000)
 ,delimiter varchar(100) = ',' --default to comma delimited.
 );
RETURNS retTable TABLE 
 (
  Position  INT GENERATED BY DEFAULT AS IDENTITY
 ,valueInt INT 
 ,valueNum Numeric(18,3)
 ,value varchar(2000)
 );
AS
/*
********************************************************************************
Purpose: Parse values from a delimited string
  & return the result as an indexed table
Copyright 1996, 1997, 2000, 2003 Clayton Groom (<A href="mailto:Clayton_Groom@hotmail.com">Clayton_Groom@hotmail.com</A>)
Posted to the public domain Aug, 2004
06-17-03 Rewritten as SQL 2000 function.
 Reworked to allow for delimiters > 1 character in length 
 and to convert Text values to numbers
********************************************************************************
*/
BEGIN
 DECLARE w_Continue  INT
  ,w_StartPos  INT
  ,w_Length  INT
  ,w_Delimeter_pos INT
  ,w_tmp_int  INT
  ,w_tmp_num  numeric(18,3)
  ,w_tmp_txt   varchar(2000)
  ,w_Delimeter_Len SMALLINT
 if len(text) = 0
 begin
  SET  w_Continue = 0 -- force early exit
 end 
 else
 begin
 -- parse the original text array into a temp table
  SET  w_Continue = 1
  SET w_StartPos = 1
  SET text = RTRIM( LTRIM( text))
  SET w_Length   = DATALENGTH( RTRIM( LTRIM( text)))
  SET w_Delimeter_Len = len(delimiter)
 end
 WHILE w_Continue = 1
 BEGIN
  SET w_Delimeter_pos = CHARINDEX( delimiter
      ,(SUBSTRING( text, w_StartPos
      ,((w_Length - w_StartPos) + w_Delimeter_Len)))
      );
 
  IF w_Delimeter_pos > 0  -- delimeter(s) found, get the value
  BEGIN
   SET w_tmp_txt = LTRIM(RTRIM( SUBSTRING( text, w_StartPos 
        ,(w_Delimeter_pos - 1)) ))
   if isnumeric(w_tmp_txt) = 1
   begin
    set w_tmp_int = cast( cast(w_tmp_txt as numeric) as INT)
    set w_tmp_num = cast( w_tmp_txt as numeric(18,3))
   end
   else
   begin
    set w_tmp_int =  null
    set w_tmp_num =  null
   end
   SET w_StartPos = w_Delimeter_pos + w_StartPos + (w_Delimeter_Len- 1)
  END
  ELSE -- No more delimeters, get last value
  BEGIN
   SET w_tmp_txt = LTRIM(RTRIM( SUBSTRING( text, w_StartPos 
      ,((w_Length - w_StartPos) + w_Delimeter_Len)) ))
   if isnumeric(w_tmp_txt) = 1
   begin
    set w_tmp_int = cast( cast(w_tmp_txt as numeric) as INT)
    set w_tmp_num = cast( w_tmp_txt as numeric(18,3))
   end
   else
   begin
    set w_tmp_int =  null
    set w_tmp_num =  null
   end
   SELECT w_Continue = 0
  END
  INSERT INTO retTable VALUES( w_tmp_int, w_tmp_num, w_tmp_txt )
 END
RETURN
END

-- File: Stored Procedures/Articles/Article_Add.sql
CREATE OR REPLACE PROCEDURE  public."Article_Add"
(
    IN feedId INT DEFAULT 0,
    IN subjects INT DEFAULT 0,
    IN subjectId INT DEFAULT 0,
    IN score smallint DEFAULT 0,
    IN domain VARCHAR(50),
    IN url VARCHAR(250),
    IN title VARCHAR(250) DEFAULT '',
    IN summary VARCHAR(250) DEFAULT '',
    filesize DOUBLE PRECISION = 0,
    IN linkcount INT DEFAULT 0,
    IN linkwordcount INT DEFAULT 0,
    IN wordcount INT DEFAULT 0,
    IN sentencecount smallint DEFAULT 0,
    IN paragraphcount smallint DEFAULT 0,
    IN importantcount smallint DEFAULT 0,
    IN yearstart smallint DEFAULT 0,
    IN yearend smallint DEFAULT 0,
    IN years VARCHAR(50) DEFAULT '',
    IN images smallint DEFAULT 0,
    IN datepublished TIMESTAMP DEFAULT NULL,
    IN relavance smallint DEFAULT 1,
    IN importance smallint DEFAULT 1,
    IN fiction smallint DEFAULT 1,
    analyzed DOUBLE PRECISION = 0.1,
    IN active BOOLEAN DEFAULT TRUE
);
LANGUAGE plpgsql
AS $$
DECLARE
    articleId INT := NULL;
    domainId INT := NULL;
    domain_results TABLE (id INT);
BEGIN
SELECT domainId = domainId FROM Domains WHERE domain=domain
SELECT articleId = articleId FROM Articles WHERE url=url
IF domainId IS NULL BEGIN
	INSERT INTO domain_results
	EXEC Domain_Add domain=domain, parentId=0
	SELECT domainId = domainId FROM Domains WHERE domain=domain
END
IF articleId IS NULL BEGIN
	SET articleId = nextval('public."SequenceArticles"')
	INSERT INTO Articles 
	(articleId, feedId, subjects, subjectId, domainId, score, domain, url, title, summary, filesize, linkcount, linkwordcount, wordcount, sentencecount, paragraphcount, importantcount, analyzecount,
	yearstart, yearend, years, images, datecreated, datepublished, relavance, importance, fiction, analyzed, active)
	VALUES 
	(articleId, feedId, subjects, subjectId, domainId, score, domain, url, title, summary, filesize, linkcount, linkwordcount, wordcount, sentencecount, paragraphcount, importantcount, 1,
	yearstart, yearend, years, images, CURRENT_TIMESTAMP, datepublished, relavance, importance, fiction, analyzed, active)
	--update domain record
	UPDATE Domains SET articles+=1 WHERE domainId=domainId
END
--archive related Download Queue record
UPDATE DownloadQueue SET status=0 WHERE "url" = url
SELECT articleId
END;

$$;

-- File: Stored Procedures/Articles/Article_Clean.sql
CREATE OR REPLACE PROCEDURE  public."Article_Clean"
(
    IN articleId INT DEFAULT 0
);
LANGUAGE plpgsql
AS $$
BEGIN
EXEC ArticleSubjects_Remove articleId=articleId
	EXEC ArticleWords_Remove articleId=articleId
RETURN 0
END;

$$;

-- File: Stored Procedures/Articles/Article_Exists.sql
CREATE OR REPLACE PROCEDURE  public."Article_Exists"
(
    IN url VARCHAR(250)
);
LANGUAGE plpgsql
AS $$
BEGIN
SELECT COUNT(*) FROM Articles WHERE url=url
END;

$$;

-- File: Stored Procedures/Articles/Article_GetById.sql
CREATE OR REPLACE PROCEDURE  public."Article_GetById"
(
    IN articleId INT
);
LANGUAGE plpgsql
AS $$
BEGIN
SELECT a.*, d.domain FROM Articles a
	LEFT JOIN Domains d ON d.domainId = a.domainId
	WHERE a.articleId=articleId
RETURN 0
END;

$$;

-- File: Stored Procedures/Articles/Article_GetByUrl.sql
CREATE OR REPLACE PROCEDURE  public."Article_GetByUrl"
(
    IN url VARCHAR(250)
);
LANGUAGE plpgsql
AS $$
BEGIN
SELECT * FROM Articles WHERE url=url
RETURN 0
END;

$$;

-- File: Stored Procedures/Articles/Article_Remove.sql
CREATE OR REPLACE PROCEDURE  public."Article_Remove"
(
    IN articleId INT DEFAULT 0
);
LANGUAGE plpgsql
AS $$
DECLARE
    domainId INT;
BEGIN
SELECT domainId = domainId FROM Articles WHERE articleId=articleId
	IF domainId IS NOT NULL BEGIN
		DELETE FROM ArticleSentences WHERE articleId=articleId
		DELETE FROM ArticleWords WHERE articleId=articleId
		DELETE FROM ArticleSubjects WHERE articleId=articleId
		/* DELETE FROM ArticleStatistics WHERE articleId=articleId */
		DELETE FROM Articles WHERE articleId=articleId
		UPDATE Domains SET articles -= 1 WHERE domainId=domainId
	END
RETURN 0
END;

$$;

-- File: Stored Procedures/Articles/Article_Update.sql
CREATE OR REPLACE PROCEDURE  public."Article_Update"
(
    IN articleId INT DEFAULT 0,
    IN subjects INT DEFAULT 0,
    IN subjectId INT DEFAULT 0,
    IN score smallint DEFAULT 0,
    IN title VARCHAR(250),
    IN summary VARCHAR(250),
    filesize DOUBLE PRECISION = 0,
    IN wordcount INT DEFAULT 0,
    IN sentencecount INT DEFAULT 0,
    IN paragraphcount INT DEFAULT 0,
    IN importantcount INT DEFAULT 0,
    IN yearstart INT DEFAULT 0,
    IN yearend INT DEFAULT 0,
    IN years VARCHAR(50),
    IN images smallint DEFAULT 0,
    IN datepublished TIMESTAMP,
    IN relavance smallint DEFAULT 1,
    IN importance smallint DEFAULT 1,
    IN fiction smallint DEFAULT 1,
    analyzed DOUBLE PRECISION = 0.1
);
LANGUAGE plpgsql
AS $$
BEGIN
UPDATE Articles SET 
subjects=subjects, subjectId=subjectId, score=score, title=title, summary=summary, filesize=filesize, wordcount=wordcount, sentencecount=sentencecount,
paragraphcount=paragraphcount, importantcount=importantcount, analyzecount=analyzecount+1, 
yearstart=yearstart, yearend=yearend, years=years, images=images, datepublished=datepublished, 
relavance=relavance, importance=importance, fiction=fiction, analyzed=analyzed
WHERE articleId=articleId
END;

$$;

-- File: Stored Procedures/Articles/Article_UpdateCache.sql
CREATE OR REPLACE PROCEDURE  public."Article_UpdateCache"
(
    IN articleId INT DEFAULT 0,
    IN cached BOOLEAN DEFAULT TRUE
);
LANGUAGE plpgsql
AS $$
BEGIN
UPDATE Articles SET cached=cached WHERE articleId=articleId
END;

$$;

-- File: Stored Procedures/Articles/Article_UpdateUrl.sql
CREATE OR REPLACE PROCEDURE  public."Article_UpdateUrl"
(
    IN articleId INT DEFAULT 0,
    IN url VARCHAR(250),
    IN domain VARCHAR(250),
    IN parentId INT DEFAULT 0
);
LANGUAGE plpgsql
AS $$
DECLARE
    oldurl VARCHAR(250), domainId INT;
    newarticleId INT;
BEGIN
SELECT oldurl="url" FROM Articles WHERE articleId=articleId
SELECT domainId=domainId FROM Domains WHERE domain=domain
IF domainId IS NULL BEGIN
	EXEC Domain_Add domain=domain, parentId=parentId
	SELECT domainId=domainId FROM Domains WHERE domain=domain
END
IF oldurl != url BEGIN
	SELECT TOP 1 newarticleId = articleId FROM Articles WHERE "url"=url ORDER BY datecreated ASC
	IF newarticleId IS NOT NULL AND newarticleId != articleId BEGIN
		DELETE FROM Articles WHERE articleId=articleId
	END
	UPDATE Articles SET "url"=url, domainId=domainId, domain=domain WHERE articleId=articleId
	--delete any downloads that already use the new URL
	DELETE FROM DownloadQueue WHERE "url"=url
	DELETE FROM Downloads WHERE "url"=url
	--update downloads that used the old URL
	UPDATE DownloadQueue SET "url"=url, domainId=domainId WHERE "url"=oldurl
	UPDATE Downloads SET "url"=url, domainId=domainId WHERE "url"=oldurl
END
END;

$$;

-- File: Stored Procedures/Articles/Article_Visited.sql
CREATE OR REPLACE PROCEDURE  public."Article_Visited"
(
    IN articleId INT
);
LANGUAGE plpgsql
AS $$
BEGIN
UPDATE Articles SET visited += 1, cached = 1 WHERE articleId=articleId
END;

$$;

-- File: Stored Procedures/Articles/ArticleBug_Add.sql
CREATE OR REPLACE PROCEDURE  public."ArticleBug_Add"
(
    IN articleId INT DEFAULT 0,
    IN title VARCHAR(100) DEFAULT '',
    IN description TEXT DEFAULT '',
    IN status SMALLINT DEFAULT 0
);
LANGUAGE plpgsql
AS $$
DECLARE
    bugId INT := nextval('public."SequenceArticleBugs"');
BEGIN
INSERT INTO ArticleBugs (bugId, articleId, title, "description", datecreated, "status")
	VALUES (bugId, articleId, title, description, CURRENT_TIMESTAMP, status)
END;

$$;

-- File: Stored Procedures/Articles/ArticleBug_UpdateDescription.sql
CREATE OR REPLACE PROCEDURE  public."ArticleBug_UpdateDescription"
(
    IN bugId INT DEFAULT 0,
    IN description TEXT DEFAULT ''
);
LANGUAGE plpgsql
AS $$
BEGIN
UPDATE ArticleBugs SET description=description WHERE bugId=bugId
END;

$$;

-- File: Stored Procedures/Articles/ArticleBug_UpdateStatus.sql
CREATE OR REPLACE PROCEDURE  public."ArticleBug_UpdateStatus"
(
    IN bugId INT DEFAULT 0,
    IN status INT DEFAULT 0
);
LANGUAGE plpgsql
AS $$
BEGIN
UPDATE ArticleBugs SET "status"=status WHERE bugId=bugId
END;

$$;

-- File: Stored Procedures/Articles/ArticleBugs_GetList.sql
CREATE OR REPLACE PROCEDURE  public."ArticleBugs_GetList"
(
    IN articleId INT DEFAULT 0,
    IN start INT DEFAULT 1,
    IN length INT DEFAULT 50,
    IN orderby INT DEFAULT 1
);
LANGUAGE plpgsql
AS $$
BEGIN
SELECT * FROM (
		SELECT ROW_NUMBER() OVER(ORDER BY 
		CASE WHEN orderby = 1 THEN "status" END ASC,
		CASE WHEN orderby = 2 THEN "status" END DESC,
		CASE WHEN orderby = 3 THEN datecreated END ASC,
		CASE WHEN orderby = 4 THEN datecreated END DESC
		) AS rownum, * FROM ArticleBugs 
			WHERE articleId = CASE WHEN articleId > 0 THEN articleId ELSE articleId END
	) AS tbl WHERE rownum >= start AND rownum < start + length
END;

$$;

-- File: Stored Procedures/Articles/ArticleDate_Add.sql
CREATE OR REPLACE PROCEDURE  public."ArticleDate_Add"
(
    IN articleId INT DEFAULT 0,
    IN date date,
    IN hasyear BOOLEAN DEFAULT FALSE,
    IN hasmonth BOOLEAN DEFAULT FALSE,
    IN hasday BOOLEAN DEFAULT FALSE
);
LANGUAGE plpgsql
AS $$
BEGIN
INSERT INTO ArticleDates (articleId, "date", hasyear, hasmonth, hasday)
	VALUES (articleId, date, hasyear, hasmonth, hasday)
RETURN 0
END;

$$;

-- File: Stored Procedures/Articles/Articles_GetCount.sql
CREATE OR REPLACE PROCEDURE  public."Articles_GetCount"
(
    IN subjectIds TEXT DEFAULT '',
    IN search TEXT DEFAULT '',
    IN feedId INT DEFAULT 0,
    IN domainId INT DEFAULT 0,
    IN score INT DEFAULT -9999,
    IN isActive INT DEFAULT 2,
    IN isDeleted BOOLEAN DEFAULT FALSE,
    IN minImages INT DEFAULT 0,
    IN dateStart TIMESTAMP(6) DEFAULT NULL,
    IN dateEnd TIMESTAMP(6) DEFAULT NULL,
    IN bugsonly BOOLEAN DEFAULT FALSE
);
LANGUAGE plpgsql
AS $$
BEGIN
/* set default dates */
	IF (dateStart IS NULL) BEGIN SET dateStart = DATEADD(YEAR, -100, CURRENT_TIMESTAMP) END
	IF (dateEnd IS NULL) BEGIN SET dateEnd = DATEADD(YEAR, 100, CURRENT_TIMESTAMP) END
	PRINT FORMAT(dateStart, 'yyyy-MM-dd HH:mm:ss.fff')
	PRINT FORMAT(dateEnd, 'yyyy-MM-dd HH:mm:ss.fff')
	/* get subjects from array */
	SELECT * INTO #subjects FROM public.SplitArray(subjectIds, ',')
	SELECT articleId INTO #subjectarticles FROM ArticleSubjects
	WHERE subjectId IN (SELECT CONVERT(INT, value) FROM #subjects)
	AND datecreated >= CONVERT(TIMESTAMP, dateStart) AND datecreated <= CONVERT(TIMESTAMP, dateEnd)
	/* get articles that match a search term */
	SELECT * INTO #search FROM public.SplitArray(search, ',')
	SELECT wordid INTO #wordids FROM Words WHERE word IN (SELECT value FROM #search)
	SELECT articleId INTO #searchedarticles FROM ArticleWords
	WHERE wordId IN (SELECT * FROM #wordids)
	/* get list of articles that match filter */
	SELECT COUNT(*)
	FROM Articles a
	WHERE
	(
		a.articleId IN (SELECT * FROM #subjectarticles)
		OR a.articleId IN (SELECT * FROM #searchedarticles)
		OR (search IS NOT NULL AND search  <> '' AND (
			a.title LIKE '%' + search + '%'
			OR a.summary LIKE '%' + search + '%'
			OR a."url" LIKE '%' + search + '%'
		))
		OR (search IS NULL OR search = '')
	); 
	AND (
		(feedId > 0 AND a.feedId = feedId)
		OR feedId = 0
	);
	AND (
		(domainId > 0 AND a.domainId = domainId)
		OR domainId = 0
	);
	AND a.active = CASE WHEN isActive = 2 THEN a.active ELSE isActive END
	AND a.deleted = isDeleted
	AND a.score >= score
	AND (
		(minImages > 0 AND a.images >= minImages)
		OR minImages <= 0
	);
	AND a.datecreated >= dateStart AND a.datecreated <= dateEnd
END;

$$;

-- File: Stored Procedures/Articles/Articles_GetList.sql
CREATE OR REPLACE PROCEDURE  public."Articles_GetList"
(
    IN subjectIds TEXT DEFAULT '',
    IN search TEXT DEFAULT '',
    IN feedId INT DEFAULT 0,
    IN domainId INT DEFAULT 0,
    IN score INT DEFAULT -9999,
    IN isActive INT DEFAULT 2,
    IN isDeleted BOOLEAN DEFAULT FALSE,
    IN minImages INT DEFAULT 0,
    IN dateStart TIMESTAMP(6) DEFAULT NULL,
    IN dateEnd TIMESTAMP(6) DEFAULT NULL,
    IN orderby INT DEFAULT 5,
    IN start INT DEFAULT 1,
    IN length INT DEFAULT 50,
    IN bugsonly BOOLEAN DEFAULT FALSE
);
LANGUAGE plpgsql
AS $$
BEGIN
/* set default dates */
	IF (dateStart IS NULL) BEGIN SET dateStart = DATEADD(YEAR, -100, CURRENT_TIMESTAMP) END
	IF (dateEnd IS NULL) BEGIN SET dateEnd = DATEADD(YEAR, 100, CURRENT_TIMESTAMP) END
	PRINT FORMAT(dateStart, 'yyyy-MM-dd HH:mm:ss.fff')
	PRINT FORMAT(dateEnd, 'yyyy-MM-dd HH:mm:ss.fff')
	/* get subjects from array */
	SELECT * INTO #subjects FROM public.SplitArray(subjectIds, ',')
	SELECT articleId INTO #subjectarticles FROM ArticleSubjects
	WHERE subjectId IN (SELECT CONVERT(INT, value) FROM #subjects)
	AND datecreated >= CONVERT(TIMESTAMP, dateStart) AND datecreated <= CONVERT(TIMESTAMP, dateEnd)
	/* get articles that match a search term */
	SELECT * INTO #search FROM public.SplitArray(search, ',')
	SELECT wordid INTO #wordids FROM Words WHERE word IN (SELECT value FROM #search)
	SELECT articleId INTO #searchedarticles FROM ArticleWords
	WHERE wordId IN (SELECT * FROM #wordids)
	/* get list of articles that match filter */
	SELECT * FROM (
		SELECT ROW_NUMBER() OVER(ORDER BY 
			CASE WHEN orderby = 0 THEN a.title END ASC,
			CASE WHEN orderby = 1 THEN a.title END DESC,
			CASE WHEN orderby = 2 THEN a."url" END ASC,
			CASE WHEN orderby = 3 THEN a."url" END DESC,
			CASE WHEN orderby = 4 THEN a.score END ASC,
			CASE WHEN orderby = 5 THEN a.score END DESC,
			CASE WHEN orderby = 6 THEN a.datecreated END DESC,
			CASE WHEN orderby = 7 THEN a.datecreated END,
			CASE WHEN orderby = 8 THEN a.visited END DESC
		) AS rownum, a.*,
		s.breadcrumb, s.hierarchy, s.title AS subjectTitle
		FROM Articles a 
		LEFT JOIN Subjects s ON s.subjectId=a.subjectId
		WHERE
		(
			a.articleId IN (SELECT * FROM #subjectarticles)
			OR a.articleId IN (SELECT * FROM #searchedarticles)
			OR (search IS NOT NULL AND search  <> '' AND (
				a.title LIKE '%' + search + '%'
				OR a.summary LIKE '%' + search + '%'
				OR a."url" LIKE '%' + search + '%'
			))
			OR (search IS NULL OR search = '')
		); 
		AND (
			(feedId > 0 AND a.feedId = feedId)
			OR feedId = 0
		);
		AND (
			(domainId > 0 AND a.domainId = domainId)
			OR domainId = 0
		);
		AND a.active = CASE WHEN isActive = 2 THEN a.active ELSE isActive END
		AND a.deleted = isDeleted
		AND a.score >= score
		AND (
			(minImages > 0 AND a.images >= minImages)
			OR minImages <= 0
		);
		AND a.datecreated >= dateStart AND a.datecreated <= dateEnd
	) AS tbl WHERE rownum >= start AND rownum < start + length
END;

$$;

-- File: Stored Procedures/Articles/ArticleSentence_Add.sql
CREATE OR REPLACE PROCEDURE  public."ArticleSentence_Add"
(
    IN articleId INT DEFAULT 0,
    IN index INT DEFAULT 0,
    IN sentence TEXT DEFAULT ''
);
LANGUAGE plpgsql
AS $$
BEGIN
INSERT INTO ArticleSentences (articleId, "index", sentence)
	VALUES (articleId, index, sentence)
RETURN 0
END;

$$;

-- File: Stored Procedures/Articles/ArticleSentences_Remove.sql
CREATE OR REPLACE PROCEDURE  public."ArticleSentences_Remove"
(
    IN articleId INT DEFAULT 0
);
LANGUAGE plpgsql
AS $$
BEGIN
DELETE FROM ArticleSentences WHERE articleId=articleId
RETURN 0
END;

$$;

-- File: Stored Procedures/Articles/ArticleSubject_Add.sql
CREATE OR REPLACE PROCEDURE  public."ArticleSubject_Add"
(
    IN articleId INT DEFAULT 0,
    IN subjectId INT DEFAULT 0,
    IN datepublished TIMESTAMP DEFAULT null,
    IN score INT DEFAULT 0
);
LANGUAGE plpgsql
AS $$
BEGIN
IF (SELECT COUNT(*) FROM ArticleSubjects WHERE articleId=articleId AND subjectId=subjectId) = 0 BEGIN
		INSERT INTO ArticleSubjects (articleId, subjectId, datecreated, datepublished, score) 
		VALUES (articleId, subjectId, CURRENT_TIMESTAMP, datepublished, score)
	END
END;

$$;

-- File: Stored Procedures/Articles/ArticleSubjects_Remove.sql
CREATE OR REPLACE PROCEDURE  public."ArticleSubjects_Remove"
(
    IN articleId INT DEFAULT 0,
    IN subjectId INT DEFAULT 0
);
LANGUAGE plpgsql
AS $$
BEGIN
IF subjectId = 0 BEGIN
		DELETE FROM ArticleSubjects WHERE articleId=articleId
	END ELSE BEGIN
		DELETE FROM ArticleSubjects WHERE articleId=articleId AND subjectId=subjectId
	END
RETURN 0
END;

$$;

-- File: Stored Procedures/Articles/ArticleWord_Add.sql
CREATE OR REPLACE PROCEDURE  public."ArticleWord_Add"
(
    IN articleId INT DEFAULT 0,
    IN wordId INT DEFAULT 0,
    IN count INT DEFAULT 0
);
LANGUAGE plpgsql
AS $$
BEGIN
IF (SELECT COUNT(*) FROM ArticleWords WHERE articleId=articleId AND wordId=wordId) = 0 BEGIN
		INSERT INTO ArticleWords (articleId, wordId, "count") 
		VALUES (articleId, wordId, count)
	END
END;

$$;

-- File: Stored Procedures/Articles/ArticleWords_Remove.sql
CREATE OR REPLACE PROCEDURE  public."ArticleWords_Remove"
(
    IN articleId INT DEFAULT 0,
    IN word VARCHAR(50) DEFAULT ''
);
LANGUAGE plpgsql
AS $$
DECLARE
    wordId INT := 0;
BEGIN
IF word = '' BEGIN
		DELETE FROM ArticleWords WHERE articleId=articleId
	END ELSE BEGIN
		SELECT wordId=wordId FROM words WHERE word=word
		DELETE FROM ArticleWords WHERE articleId=articleId AND wordId=wordId
	END
RETURN 0
END;

$$;

-- File: Stored Procedures/Blacklist/Blacklist_Domain_Add.sql
CREATE OR REPLACE PROCEDURE  public."Blacklist_Domain_Add"
(
    IN domain VARCHAR(64)
);
LANGUAGE plpgsql
AS $$
DECLARE
    domainId INT;
BEGIN TRY
	INSERT INTO Blacklist_Domains (domain) VALUES (domain)
	END TRY
	BEGIN CATCH
	END CATCH
	SELECT domainId=domainId FROM Domains WHERE domain=domain
	-- delete all articles related to domain
	EXEC Domain_DeleteAllArticles domainId=domainId
	--delete all download queue related to domain
	DELETE FROM DownloadQueue WHERE domainId=domainId
	DELETE FROM Downloads WHERE domainId=domainId
	DELETE FROM Domains WHERE domainId=domainId
	--delete whitelisted domains (if any)
	DELETE FROM Whitelist_Domains WHERE domain=domain
END;

$$;

-- File: Stored Procedures/Blacklist/Blacklist_Domain_Check.sql
CREATE OR REPLACE PROCEDURE  public."Blacklist_Domain_Check"
(
    IN domain VARCHAR(64)
);
LANGUAGE plpgsql
AS $$
BEGIN
SELECT COUNT(*) FROM Blacklist_Domains WHERE domain=domain
END;

$$;

-- File: Stored Procedures/Blacklist/Blacklist_Domain_Rermove.sql
CREATE OR REPLACE PROCEDURE  public."Blacklist_Domain_Remove"
(
    IN domain VARCHAR(64)
);
LANGUAGE plpgsql
AS $$
BEGIN
DELETE FROM Blacklist_Domains WHERE domain=domain
END;

$$;

-- File: Stored Procedures/Blacklist/Blacklist_Domains_CheckAll.sql
CREATE OR REPLACE PROCEDURE  public."Blacklist_Domains_CheckAll"
(
    IN domains TEXT
);
LANGUAGE plpgsql
AS $$
BEGIN
SELECT * INTO #domains FROM public.SplitArray(domains, ',')
	SELECT domain FROM Blacklist_Domains WHERE domain IN (SELECT "value" FROM #domains)
	DROP TABLE #domains
END;

$$;

-- File: Stored Procedures/Blacklist/Blacklist_Domains_GetList.sql
CREATE OR REPLACE PROCEDURE public."Blacklist_Domains_GetList"
LANGUAGE plpgsql
AS $$
BEGIN
SELECT domain FROM Blacklist_Domains ORDER BY domain ASC
END;

$$;

-- File: Stored Procedures/Blacklist/Blacklist_Wildcard_Add.sql
CREATE OR REPLACE PROCEDURE  public."Blacklist_Wildcard_Add"
(
    IN domain VARCHAR(64)
);
LANGUAGE plpgsql
AS $$
BEGIN
INSERT INTO Blacklist_Wildcards (domain) VALUES (domain)
END;

$$;

-- File: Stored Procedures/Blacklist/Blacklist_Wildcard_Rermove.sql
CREATE OR REPLACE PROCEDURE  public."Blacklist_Wildcard_Remove"
(
    IN domain VARCHAR(64)
);
LANGUAGE plpgsql
AS $$
BEGIN
DELETE FROM Blacklist_Wildcards WHERE domain=domain
END;

$$;

-- File: Stored Procedures/Blacklist/Blacklist_Wildcards_GetList.sql
CREATE OR REPLACE PROCEDURE public."Blacklist_Wildcards_GetList"
LANGUAGE plpgsql
AS $$
BEGIN
SELECT domain FROM Blacklist_Wildcards ORDER BY domain ASC
END;

$$;

-- File: Stored Procedures/DestroyCollection.sql
CREATE OR REPLACE PROCEDURE  public."DestroyCollection"
(
    IN articles BOOLEAN DEFAULT TRUE,
    IN subjects BOOLEAN DEFAULT TRUE,
    IN topics BOOLEAN DEFAULT TRUE
);
LANGUAGE plpgsql
AS $$
BEGIN
IF articles = 1 OR subjects = 1 BEGIN
		DELETE FROM ArticleBugs
		DELETE FROM ArticleDates
		DELETE FROM Articles
		DELETE FROM ArticleSentences
		DELETE FROM ArticleSubjects
		DELETE FROM ArticleWords
		DELETE FROM DownloadQueue
		DELETE FROM Downloads
		DELETE FROM FeedsCheckedLog
	END
	IF subjects = 1 BEGIN
		DELETE FROM Subjects
		DELETE FROM Words
	END
END;

$$;

-- File: Stored Procedures/Domains/AnalyzerRules/Domain_AnalyzerRule_Add.sql
CREATE OR REPLACE PROCEDURE  public."Domain_AnalyzerRule_Add"
(
    IN domainId INT,
    IN selector varchar(64) DEFAULT '',
    IN rule BOOLEAN DEFAULT FALSE
);
LANGUAGE plpgsql
AS $$
DECLARE
    id INT := nextval('public."SequenceAnalyzerRules"');
BEGIN
INSERT INTO AnalyzerRules (ruleId, domainId, selector, "rule", datecreated)
	VALUES (id, domainId, selector, rule, CURRENT_TIMESTAMP)
	SELECT id
END;

$$;

-- File: Stored Procedures/Domains/AnalyzerRules/Domain_AnalyzerRule_Remove.sql
CREATE OR REPLACE PROCEDURE  public."Domain_AnalyzerRule_Remove"
(
    IN ruleId INT
);
LANGUAGE plpgsql
AS $$
BEGIN
DELETE FROM AnalyzerRules WHERE ruleId=ruleId
END;

$$;

-- File: Stored Procedures/Domains/AnalyzerRules/Domain_AnalyzerRules_GetList.sql
CREATE OR REPLACE PROCEDURE  public."Domain_AnalyzerRules_GetList"
(
    IN domainId INT
);
LANGUAGE plpgsql
AS $$
BEGIN
SELECT * FROM AnalyzerRules WHERE domainId=domainId ORDER BY datecreated ASC
END;

$$;

-- File: Stored Procedures/Domains/Clean/Domain_CleanDownloads.sql
CREATE OR REPLACE PROCEDURE  public."Domain_CleanDownloads"
(
    IN domainId INT
);
LANGUAGE plpgsql
AS $$
DECLARE
    articles TABLE (articleId INT);
    a_total INT, b_total INT;
BEGIN
--get all article that will be deleted
	INSERT INTO articles 
	SELECT DISTINCT a.articleId
	FROM Articles a
	JOIN DownloadRules r ON r.domainId = a.domainId
	WHERE a.domainId=domainId 
	AND (
		(LEN(r."url") > 0 AND a."url" LIKE '%' + r."url" + '%')
		OR (LEN(r.title) > 0 AND a.title LIKE '%' + r.title + '%')
		OR (LEN(r.summary) > 0 AND a.summary LIKE '%' + r.summary + '%')
	);
	-- delete all associated articles
	DELETE FROM Articles WHERE articleId IN (SELECT articleId FROM articles)
	DELETE FROM ArticleBugs WHERE articleId IN (SELECT articleId FROM articles)
	DELETE FROM ArticleDates WHERE articleId IN (SELECT articleId FROM articles)
	DELETE FROM ArticleSentences WHERE articleId IN (SELECT articleId FROM articles)
	DELETE FROM ArticleSubjects WHERE articleId IN (SELECT articleId FROM articles)
	DELETE FROM ArticleWords WHERE articleId IN (SELECT articleId FROM articles)
	-- delete all associated download queue records
	DELETE FROM DownloadQueue WHERE qId IN (
		SELECT DISTINCT dq.qid FROM DownloadQueue dq
		JOIN DownloadRules r ON r.domainId = dq.domainId
		WHERE dq.domainId=domainId 
		AND (
			(LEN(r."url") > 0 AND r."rule"=0 AND dq."url" LIKE '%' + r."url" + '%')
		);
	);
	-- delete all associated download archive records
	DELETE FROM Downloads WHERE id IN (
		SELECT DISTINCT d.id FROM Downloads d
		JOIN DownloadRules r ON r.domainId = d.domainId
		WHERE d.domainId=domainId 
		AND (
			(LEN(r."url") > 0 AND r."rule"=0 AND d."url" LIKE '%' + r."url" + '%')
		);
	);
END;

$$;

-- File: Stored Procedures/Domains/Clean/Domain_GetDownloadsToClean.sql
CREATE OR REPLACE PROCEDURE  public."Domain_GetDownloadsToClean"
(
    IN domainId INT,
    IN topten BOOLEAN DEFAULT FALSE
);
LANGUAGE plpgsql
AS $$
DECLARE
    articles TABLE (articleId INT);
    a_total INT, b_total INT;
BEGIN
--get all article that will be deleted
	INSERT INTO articles 
	SELECT DISTINCT a.articleId
	FROM Articles a
	JOIN DownloadRules r ON r.domainId = a.domainId
	WHERE a.domainId=domainId 
	AND 
	(
		( -- download rules
			(LEN(r."url") > 0 AND a."url" LIKE '%' + r."url" + '%')
			OR (LEN(r.title) > 0 AND a.title LIKE '%' + r.title + '%')
			OR (LEN(r.summary) > 0 AND a.summary LIKE '%' + r.summary + '%')
		);
	);
	-- #1: count of all affected articles
	SELECT COUNT(*) AS total FROM articles
	-- #2: details about affected articles
	IF topten = 1 BEGIN
		SELECT TOP 10 articleId, "title", "url" FROM Articles WHERE articleId IN (SELECT * FROM articles)
	END ELSE BEGIN
		SELECT articleId, "title", "url" FROM Articles WHERE articleId IN (SELECT * FROM articles)
	END
	-- #3: total affected downloads from download queue & download archive
	SELECT a_total = COUNT(*) FROM (
		SELECT DISTINCT dq.qid FROM DownloadQueue dq
		JOIN DownloadRules r ON r.domainId = dq.domainId
		WHERE dq.domainId=domainId 
		AND (
			(LEN(r."url") > 0 AND r."rule"=0 AND dq."url" LIKE '%' + r."url" + '%')
		);
	) AS tbl
	SELECT b_total = COUNT(*) FROM (
		SELECT DISTINCT d.id FROM Downloads d
		JOIN DownloadRules r ON r.domainId = d.domainId
		WHERE d.domainId=domainId 
		AND (
			(LEN(r."url") > 0 AND r."rule"=0 AND d."url" LIKE '%' + r."url" + '%')
		);
	) AS tbl
	SELECT a_total + b_total AS total
END;

$$;

-- File: Stored Procedures/Domains/CollectionGroups/Domain_CollectionGroup_Add.sql
CREATE OR REPLACE PROCEDURE  public."Domain_CollectionGroup_Add"
(
    IN name VARCHAR(32)
);
LANGUAGE plpgsql
AS $$
DECLARE
    id INT := nextval('public."SequenceDomainCollectionGroups"');
BEGIN
INSERT INTO DomainCollectionGroups (colgroupId, "name")
	VALUES (id, name)
	SELECT id
END;

$$;

-- File: Stored Procedures/Domains/CollectionGroups/Domain_CollectionGroup_Remove.sql
CREATE OR REPLACE PROCEDURE  public."Domain_CollectionGroup_Remove"
(
    IN colgroupId INT DEFAULT 0
);
LANGUAGE plpgsql
AS $$
BEGIN
DELETE FROM DomainCollectionGroups WHERE colgroupId=colgroupId
END;

$$;

-- File: Stored Procedures/Domains/CollectionGroups/Domain_CollectionGroups_GetList.sql
CREATE OR REPLACE PROCEDURE public."Domain_CollectionGroups_GetList"
LANGUAGE plpgsql
AS $$
BEGIN
SELECT * FROM DomainCollectionGroups ORDER BY "name" ASC
END;

$$;

-- File: Stored Procedures/Domains/Collections/Domain_Collection_Add.sql
CREATE OR REPLACE PROCEDURE  public."Domain_Collection_Add"
(
    IN colgroupId INT DEFAULT 0,
    IN name VARCHAR(32),
    IN search VARCHAR(128),
    IN subjectId INT DEFAULT 0,
    IN filtertype INT DEFAULT 0,
    IN type INT DEFAULT 0,
    IN sort INT DEFAULT 0,
    IN lang varchar(6) DEFAULT ''
);
LANGUAGE plpgsql
AS $$
DECLARE
    id INT := nextval('public."SequenceDomainCollections"');
BEGIN
INSERT INTO DomainCollections (colId, colgroupId, "name", "search", subjectId, filtertype, "type", "sort", lang, datecreated)
	VALUES (id, colgroupId, name, search, subjectId, filtertype, type, sort, lang, CURRENT_TIMESTAMP)
	SELECT id
END;

$$;

-- File: Stored Procedures/Domains/Collections/Domain_Collection_Remove.sql
CREATE OR REPLACE PROCEDURE  public."Domain_Collection_Remove"
(
    IN colId INT DEFAULT 0
);
LANGUAGE plpgsql
AS $$
BEGIN
DELETE FROM DomainCollections WHERE colId=colId
END;

$$;

-- File: Stored Procedures/Domains/Collections/Domain_Collections_GetList.sql
CREATE OR REPLACE PROCEDURE public."Domain_Collections_GetList"
LANGUAGE plpgsql
AS $$
BEGIN
SELECT c.* FROM DomainCollections c
	LEFT JOIN DomainCollectionGroups g ON g.colgroupId=c.colgroupId
	ORDER BY g."name" ASC, c."name" ASC
	SELECT * FROM DomainCollectionGroups
END;

$$;

-- File: Stored Procedures/Domains/Delete/Domain_Delete.sql
CREATE OR REPLACE PROCEDURE  public."Domain_Delete"
(
    IN domainId INT
);
LANGUAGE plpgsql
AS $$
DECLARE
    domain VARCHAR(128);
BEGIN
SELECT domain = domain FROM Domains WHERE domainId=domainId
	EXEC Domain_DeleteAllArticles domainId=domainId
	DELETE FROM Domains WHERE domainId=domainId
	DELETE FROM DownloadQueue WHERE domainId=domainId
	DELETE FROM Downloads WHERE domainId=domainId
	DELETE FROM Whitelist_Domains WHERE domain=domain
	DELETE FROM Blacklist_Domains WHERE domain=domain
END;

$$;

-- File: Stored Procedures/Domains/Delete/Domain_DeleteAllArticles.sql
CREATE OR REPLACE PROCEDURE  public."Domain_DeleteAllArticles"
(
    IN domainId INT
);
LANGUAGE plpgsql
AS $$
BEGIN
SELECT articleId INTO #articles FROM Articles WHERE domainId=domainId
	DELETE FROM ArticleSentences WHERE articleId IN (SELECT * FROM #articles)
	DELETE FROM ArticleWords WHERE articleId IN (SELECT * FROM #articles)
	DELETE FROM ArticleSubjects WHERE articleId IN (SELECT * FROM #articles)
	/* DELETE FROM ArticleStatistics articleId IN (SELECT * FROM #articles) */
	DELETE FROM Articles WHERE articleId IN (SELECT * FROM #articles)
END;

$$;

-- File: Stored Procedures/Domains/Domain_Add.sql
CREATE OR REPLACE PROCEDURE  public."Domain_Add"
(
    IN domain VARCHAR(64),
    IN title VARCHAR(128) DEFAULT '',
    IN parentId INT DEFAULT 0,
    IN type INT DEFAULT 0 -- 0 = none, 1 = whitelist, 2 = blacklist
);
LANGUAGE plpgsql
AS $$
DECLARE
    id INT := nextval('public."SequenceDomains"');
    level INT;
    url TEXT := 'http://' + domain;
BEGIN
INSERT INTO Domains (domainId, parentId, domain, title, lastchecked)
	VALUES (id, parentId, domain, title, DATEADD(HOUR, -1, CURRENT_TIMESTAMP))
	SELECT id
	IF parentId > 0 BEGIN
		BEGIN TRY
			INSERT INTO DomainHierarchy (domainId, parentId, "level")
			SELECT id, parentId, "level"
			FROM DomainHierarchy WHERE domainId = parentId
		END TRY BEGIN CATCH END CATCH
		SELECT level = ISNULL(MAX("level"), 0) + 1 FROM DomainHierarchy WHERE domainId = parentId
		BEGIN TRY
			INSERT INTO DomainHierarchy (domainId, parentId, "level")
			VALUES (id, parentId, level)
		END TRY BEGIN CATCH END CATCH
		EXEC DomainLink_Add domainId=parentId, linkId=id
	END
	IF type = 1 EXEC Whitelist_Domain_Add domain=domain
	IF type = 2 EXEC Blacklist_Domain_Add domain=domain
	EXEC DownloadQueue_Add url=url, domain=domain, parentId=parentId, feedId=0
END;

$$;

-- File: Stored Procedures/Domains/DomainTypeMatches/DomainTypeMatches_Add.sql
CREATE OR REPLACE PROCEDURE  public."DomainTypeMatches_Add"
(
    IN type INT,
    IN type2 INT DEFAULT -1,
    IN words TEXT,
    IN threshold INT,
    IN rank INT
);
LANGUAGE plpgsql
AS $$
DECLARE
    id INT;
BEGIN
SET id = nextval('public."SequenceDomainTypeMatches"')
	INSERT INTO DomainTypeMatches (matchId, "type", "type2", words, threshold, "rank")
	VALUES (id, type, type2, words, threshold, rank)
END;

$$;

-- File: Stored Procedures/Domains/DomainTypeMatches/DomainTypeMatches_GetList.sql
CREATE OR REPLACE PROCEDURE public."DomainTypeMatches_GetList"
LANGUAGE plpgsql
AS $$
BEGIN
SELECT * FROM DomainTypeMatches
END;

$$;

-- File: Stored Procedures/Domains/DomainTypeMatches/DomainTypeMatches_Remove.sql
CREATE OR REPLACE PROCEDURE  public."DomainTypeMatches_Remove"
(
    IN matchId INT
);
LANGUAGE plpgsql
AS $$
BEGIN
DELETE FROM DomainTypeMatches WHERE matchId=matchId
END;

$$;

-- File: Stored Procedures/Domains/DownloadRules/Domain_DownloadRule_Add.sql
CREATE OR REPLACE PROCEDURE  public."Domain_DownloadRule_Add"
(
    IN domainId INT,
    IN rule BOOLEAN DEFAULT FALSE,
    IN url varchar(64) DEFAULT '',
    IN title varchar(64) DEFAULT '',
    IN summary varchar(64) DEFAULT ''
);
LANGUAGE plpgsql
AS $$
DECLARE
    id INT := nextval('public."SequenceDownloadRules"');
BEGIN
INSERT INTO DownloadRules (ruleId, domainId, "rule", "url", title, summary, datecreated)
	VALUES (id, domainId, rule, url, title, summary, CURRENT_TIMESTAMP)
	SELECT id
END;

$$;

-- File: Stored Procedures/Domains/DownloadRules/Domain_DownloadRule_GetArticles.sql
CREATE OR REPLACE PROCEDURE  public."Domain_DownloadRule_GetArticles"
(
    IN ruleId INT
);
LANGUAGE plpgsql
AS $$
DECLARE
    domainId INT, url VARCHAR(64), title VARCHAR(64), summary VARCHAR(64);
BEGIN
SELECT domainId = domainId, url = "url", title = "title", summary = "summary" FROM DownloadRules WHERE ruleId = ruleId
	SELECT articleId
	FROM Articles 
	WHERE domainId=domainId 
	AND (
		"url" LIKE url
		OR (LEN(title) > 0 AND title LIKE title)
		OR (LEN(summary) > 0 AND summary LIKE summary)
	);
END;

$$;

-- File: Stored Procedures/Domains/DownloadRules/Domain_DownloadRule_GetDownloads.sql
CREATE OR REPLACE PROCEDURE  public."Domain_DownloadRule_GetDownloads"
(
    IN ruleId INT
);
LANGUAGE plpgsql
AS $$
DECLARE
    domainId INT, url VARCHAR(64);
BEGIN
SELECT domainId = domainId, url = "url" FROM DownloadRules WHERE ruleId = ruleId
	SELECT qid
	FROM DownloadQueue 
	WHERE domainId=domainId 
	AND (
		"url" LIKE url
	);
END;

$$;

-- File: Stored Procedures/Domains/DownloadRules/Domain_DownloadRule_Remove.sql
CREATE OR REPLACE PROCEDURE  public."Domain_DownloadRule_Remove"
(
    IN ruleId INT
);
LANGUAGE plpgsql
AS $$
BEGIN
DELETE FROM DownloadRules WHERE ruleId=ruleId
END;

$$;

-- File: Stored Procedures/Domains/DownloadRules/Domain_DownloadRules_GetForDomains.sql
CREATE OR REPLACE PROCEDURE  public."Domain_DownloadRules_GetForDomains"
(
    IN domains TEXT
);
LANGUAGE plpgsql
AS $$
BEGIN
SELECT * INTO #domain_names FROM public.SplitArray(domains, ',')
	SELECT r.*, d.domain FROM "Domains" d
	JOIN DownloadRules r ON r.domainId = d.domainId
	WHERE d.domain IN (SELECT value FROM #domain_names)
	ORDER BY d.domainId ASC
END;

$$;

-- File: Stored Procedures/Domains/DownloadRules/Domain_DownloadRules_GetList.sql
CREATE OR REPLACE PROCEDURE  public."Domain_DownloadRules_GetList"
(
    IN domainId INT
);
LANGUAGE plpgsql
AS $$
BEGIN
SELECT * FROM DownloadRules WHERE domainId=domainId ORDER BY datecreated ASC
END;

$$;

-- File: Stored Procedures/Domains/Get/Domain_GetById.sql
CREATE OR REPLACE PROCEDURE  public."Domain_GetById"
(
    IN domainId INT
);
LANGUAGE plpgsql
AS $$
BEGIN
SELECT d.*, (SELECT COUNT(*) FROM Articles a WHERE a.domainId = domainId) AS articles,
	CASE WHEN EXISTS(SELECT * FROM Whitelist_Domains WHERE domain=d.domain) THEN 1 ELSE 0 END AS whitelisted,
	CASE WHEN EXISTS(SELECT * FROM Blacklist_Domains WHERE domain=d.domain) THEN 1 ELSE 0 END AS blacklisted
	FROM "Domains" d
	WHERE d.domainId=domainId
END;

$$;

-- File: Stored Procedures/Domains/Get/Domain_GetInfo.sql
CREATE OR REPLACE PROCEDURE  public."Domain_GetInfo"
(
    IN domain VARCHAR(64)
);
LANGUAGE plpgsql
AS $$
BEGIN
SELECT d.*, (SELECT COUNT(*) FROM Articles a WHERE a.domainId = d.domainId) AS articles,
	CASE WHEN EXISTS(SELECT * FROM Whitelist_Domains WHERE domain=domain) THEN 1 ELSE 0 END AS whitelisted,
	CASE WHEN EXISTS(SELECT * FROM Blacklist_Domains WHERE domain=d.domain) THEN 1 ELSE 0 END AS blacklisted
	FROM "Domains" d
	WHERE d.domain=domain
END;

$$;

-- File: Stored Procedures/Domains/Get/Domains_GetCount.sql
CREATE OR REPLACE PROCEDURE  public."Domains_GetCount"
(
    IN subjectIds TEXT DEFAULT '',
    IN lang varchar(6) DEFAULT '',
    IN search TEXT DEFAULT '',
    IN type INT DEFAULT 0, -- 0 = all, 1 = whitelisted, 2 = blacklisted, 3 = not-listed, 4 = paywall, 5 = free, 6 = unprocessed, 7 = empty,
    IN domainType INT DEFAULT -1,
    IN domainType2 INT DEFAULT -1,
    IN sort INT DEFAULT 0, -- 0 = ASC, 1 = DESC, 2 = most articles, 3 = newest, 4 = oldest, 5 = last updated,
    IN parentId INT DEFAULT -1,
    IN serviceIds TEXT DEFAULT NULL
);
LANGUAGE plpgsql
AS $$
DECLARE
    haswildcard BOOLEAN := 0;
BEGIN
/* get subjects from array */
	SELECT * INTO #subjects FROM public.SplitArray(subjectIds, ',')
	/* get domains that match a search term */
	SELECT * INTO #search FROM public.SplitArray(search, ',')
	IF CHARINDEX('%', search) > 0 SET haswildcard = 1
	IF type = 2 BEGIN
		/* //////////////////////////////////////////////////////////////////////////////////////// */
		/* Get domains from Blacklist table */
		/* //////////////////////////////////////////////////////////////////////////////////////// */
		SELECT COUNT(*)
		FROM "Blacklist_Domains" d
		WHERE
		(
			(search IS NOT NULL AND search  <> '' AND (
				d.domain LIKE CASE WHEN haswildcard = 1 THEN search ELSE '%' + search + '%' END
			))
			OR (search IS NULL OR search = '')
		);
	END ELSE IF type = 8 BEGIN
		/* //////////////////////////////////////////////////////////////////////////////////////// */
		/* Get domains from Blacklist Wildcards table */
		/* //////////////////////////////////////////////////////////////////////////////////////// */
		SELECT COUNT(*)
		FROM "Blacklist_Wildcards" d
		WHERE
		(
			(search IS NOT NULL AND search  <> '' AND (
				d.domain LIKE CASE WHEN haswildcard = 1 THEN search ELSE '%' + search + '%' END
			))
			OR (search IS NULL OR search = '')
		);
	END ELSE BEGIN
		/* //////////////////////////////////////////////////////////////////////////////////////// */
		/* Get domains from Domains table */
		/* //////////////////////////////////////////////////////////////////////////////////////// */
		SELECT CAST(value AS INT) AS serviceId 
		INTO #serviceIds
		FROM STRING_SPLIT(serviceIds, ',')
		SELECT COUNT(*)
		FROM "Domains" d
		LEFT JOIN Whitelist_Domains wl ON wl.domain = d.domain
		LEFT JOIN Blacklist_Domains bl ON bl.domain = d.domain
		WHERE
		(
			(search IS NOT NULL AND search  <> '' AND (
				d.title LIKE CASE WHEN haswildcard = 1 THEN search ELSE '%' + search + '%' END
				OR d.domain LIKE CASE WHEN haswildcard = 1 THEN search ELSE '%' + search + '%' END
			))
			OR (search IS NULL OR search = '')
		) AND (
			(type = 0)
			OR (type = 1 AND wl.domain IS NOT NULL)
			OR (type = 2 AND bl.domain IS NOT NULL)
			OR (type = 3 AND wl.domain IS NULL AND bl.domain IS NULL)
			OR (type = 4 AND d.paywall = 1)
			OR (type = 5 AND d.free = 1)
			OR (type = 6 AND d.free = 0 AND d.paywall = 0 AND d.type = -1 AND bl.domain IS NULL AND wl.domain IS NULL)
			OR (type = 7 AND d."empty" = 1)
			OR (type = 9 AND d."empty" = 0)
		);
		AND (
			(sort = 2 AND d.articles > 0)
			OR (sort <> 2)
		);
		AND (
				(domainType >= 0 AND domainType2 < 0 AND (d."type" = domainType OR d."type2" = domainType))
				OR 
				(domainType < 0 AND domainType2 >= 0 AND (d."type" = domainType2 OR d."type2" = domainType2))
				OR 
				(domainType >= 0 AND domainType2 >= 0 AND (d."type" = domainType OR d."type2" = domainType
														  OR d."type" = domainType2 OR d."type2" = domainType2))
				OR 
				(domainType < 0)
			);
		AND (
			(parentId >= 0 AND d.parentId = parentId)
			OR (parentId < 0)
		);
		AND (
			(lang != '' AND d.lang = lang)
			OR lang IS NULL OR lang = ''
		);
		AND d.deleted = 0
		AND (
			serviceIds IS NULL
			OR EXISTS (
				SELECT 1 FROM public."DomainServices" ds
				INNER JOIN #serviceIds s ON ds.serviceId = s.serviceId
				WHERE ds.domainId = d.domainId
			);
		);
		DROP TABLE #serviceIds
	END
END;

$$;

-- File: Stored Procedures/Domains/Get/Domains_GetList.sql
CREATE OR REPLACE PROCEDURE  public."Domains_GetList"
(
    IN subjectIds TEXT DEFAULT '',
    IN lang varchar(6) DEFAULT '',
    IN search TEXT DEFAULT '',
    IN type INT DEFAULT 0, -- 0 = all, 1 = whitelisted, 2 = blacklisted, 3 = not-listed, 4 = paywall, 5 = free, 6 = unprocessed, 7 = empty,
    IN domainType INT DEFAULT -1,
    IN domainType2 INT DEFAULT -1,
    IN sort INT DEFAULT 0, -- 0 = Domain ASC, 1 = Domain DESC, 2 = Articles DESC, 3 = DateCreated DESC, 4 = DateCreated ASC, 5 = DateUpdated DESC,
    IN start INT DEFAULT 1,
    IN length INT DEFAULT 50,
    IN parentId INT DEFAULT -1,
    IN serviceIds TEXT DEFAULT NULL
);
LANGUAGE plpgsql
AS $$
DECLARE
    haswildcard BOOLEAN := 0;
BEGIN
/* get subjects from array */
	SELECT * INTO #subjects FROM public.SplitArray(subjectIds, ',')
	/* get domains that match a search term */
	SELECT * INTO #search FROM public.SplitArray(search, ',')
	IF CHARINDEX('%', search) > 0 SET haswildcard = 1
	--PRINT 'has wildcard = ' + CONVERT(VARCHAR(1), haswildcard)
	IF type = 2 BEGIN
		/* //////////////////////////////////////////////////////////////////////////////////////// */
		/* Get domains from Blacklist table */
		/* //////////////////////////////////////////////////////////////////////////////////////// */
		SELECT * FROM (
			SELECT ROW_NUMBER() OVER(ORDER BY 
			CASE WHEN sort = 0 OR sort = 6 THEN d.domain END,
			CASE WHEN sort = 1 OR sort = 7 THEN d.domain END DESC
			) AS rownum, d.domain, -1 AS "type"
			FROM "Blacklist_Domains" d
			WHERE
			(
				(search IS NOT NULL AND search  <> '' AND (
					d.domain LIKE CASE WHEN haswildcard = 1 THEN search ELSE '%' + search + '%' END
				))
				OR (search IS NULL OR search = '')
			);
		) AS tbl WHERE rownum >= start AND rownum < start + length
	END ELSE IF type = 8 BEGIN
		/* //////////////////////////////////////////////////////////////////////////////////////// */
		/* Get domains from Blacklist Wildcards table */
		/* //////////////////////////////////////////////////////////////////////////////////////// */
		SELECT * FROM (
			SELECT ROW_NUMBER() OVER(ORDER BY 
			CASE WHEN sort = 0 OR sort = 6 THEN d.domain END,
			CASE WHEN sort = 1 OR sort = 7 THEN d.domain END DESC
			) AS rownum, d.domain, -2 AS "type"
			FROM "Blacklist_Wildcards" d
			WHERE
			(
				(search IS NOT NULL AND search  <> '' AND (
					d.domain LIKE CASE WHEN haswildcard = 1 THEN search ELSE '%' + search + '%' END
				))
				OR (search IS NULL OR search = '')
			);
		) AS tbl WHERE rownum >= start AND rownum < start + length
	END ELSE BEGIN
		/* //////////////////////////////////////////////////////////////////////////////////////// */
		/* Get domains from Domains table */
		/* //////////////////////////////////////////////////////////////////////////////////////// */
		SELECT CAST(value AS INT) AS serviceId 
		INTO #serviceIds
		FROM STRING_SPLIT(serviceIds, ',')
		SELECT * FROM (
			SELECT ROW_NUMBER() OVER(ORDER BY 
			-- Title sorting with hastitle priority
			CASE WHEN sort = 0 OR sort = 1 OR sort = 6 OR sort = 7 THEN d.hastitle END DESC, 
			CASE WHEN sort = 6 THEN d.title END,
			CASE WHEN sort = 7 THEN d.title END DESC,
			-- Domain sorting
			CASE WHEN sort = 0 THEN d.domain END,
			CASE WHEN sort = 1 THEN d.domain END DESC,
			-- Articles sorting
			CASE WHEN sort = 2 THEN d.articles END DESC,
			CASE WHEN sort = 8 THEN d.articles END ASC,
			-- Date created sorting
			CASE WHEN sort = 3 THEN d.datecreated END DESC,
			CASE WHEN sort = 4 THEN d.datecreated END ASC,
			-- Date updated sorting
			CASE WHEN sort = 5 THEN d.dateupdated END DESC,
			CASE WHEN sort = 9 THEN d.dateupdated END ASC,
			-- Status sorting (whitelisted/blacklisted status)
			CASE WHEN sort = 10 THEN 
				CASE 
					WHEN wl.domain IS NOT NULL THEN 1
					WHEN bl.domain IS NOT NULL THEN 2
					ELSE 3
				END
			END ASC,
			CASE WHEN sort = 11 THEN 
				CASE 
					WHEN wl.domain IS NOT NULL THEN 1
					WHEN bl.domain IS NOT NULL THEN 2
					ELSE 3
				END
			END DESC
			) AS rownum, d.*,
			(CASE WHEN wl.domain IS NOT NULL THEN 1 ELSE 0 END) AS whitelisted,
			(CASE WHEN bl.domain IS NOT NULL THEN 1 ELSE 0 END) AS blacklisted
			FROM "Domains" d
			LEFT JOIN Whitelist_Domains wl ON wl.domain = d.domain
			LEFT JOIN Blacklist_Domains bl ON bl.domain = d.domain
			LEFT JOIN DomainServices ds ON ds.domainId = d.domainId AND ds.serviceId IN (SELECT * FROM #serviceIds)
			WHERE
			(
				(search IS NOT NULL AND search  <> '' AND (
					d.title LIKE CASE WHEN haswildcard = 1 THEN search ELSE '%' + search + '%' END
					OR d.domain LIKE CASE WHEN haswildcard = 1 THEN search ELSE '%' + search + '%' END
				))
				OR (search IS NULL OR search = '')
			) AND (
				(type = 0)
				OR (type = 1 AND wl.domain IS NOT NULL)
				OR (type = 2 AND bl.domain IS NOT NULL)
				OR (type = 3 AND wl.domain IS NULL AND bl.domain IS NULL)
				OR (type = 4 AND d.paywall = 1)
				OR (type = 5 AND d.free = 1)
				OR (type = 6 AND d.free = 0 AND d.paywall = 0 AND d.type = -1 AND bl.domain IS NULL AND wl.domain IS NULL)
				OR (type = 7 AND d."empty" = 1)
				OR (type = 9 AND d."empty" = 0)
			);
			AND (
				(sort = 2 AND d.articles > 0)
				OR (sort <> 2)
			);
			AND (
				(domainType >= 0 AND domainType2 < 0 AND (d."type" = domainType OR d."type2" = domainType))
				OR 
				(domainType < 0 AND domainType2 >= 0 AND (d."type" = domainType2 OR d."type2" = domainType2))
				OR 
				(domainType >= 0 AND domainType2 >= 0 AND (d."type" = domainType OR d."type2" = domainType
														  OR d."type" = domainType2 OR d."type2" = domainType2))
				OR 
				(domainType < 0)
			);
			AND (
				(parentId >= 0 AND d.parentId = parentId)
				OR (parentId < 0)
			);
			AND (
				(lang != '' AND d.lang = lang)
				OR lang IS NULL OR lang = ''
			);
			AND d.deleted = 0
			AND (
				serviceIds IS NULL
				OR ds.serviceId IS NOT NULL
			);
		) AS tbl WHERE rownum >= start AND rownum < start + length
	END
END;

$$;

-- File: Stored Procedures/Domains/Links/DomainLink_Add.sql
CREATE OR REPLACE PROCEDURE  public."DomainLink_Add"
(
    IN domainId INT,
    IN linkId INT
);
LANGUAGE plpgsql
AS $$
BEGIN TRY
		INSERT INTO DomainLinks (domainId, linkId) VALUES (domainId, linkId)
	END TRY BEGIN CATCH END CATCH
END;

$$;

-- File: Stored Procedures/Domains/Links/DomainLinks_GetList.sql
CREATE OR REPLACE PROCEDURE  public."DomainLinks_GetList"
(
    IN domainId INT
);
LANGUAGE plpgsql
AS $$
BEGIN
SELECT d.*,
	(CASE WHEN wl.domain IS NOT NULL THEN 1 ELSE 0 END) AS whitelisted,
	(CASE WHEN bl.domain IS NOT NULL THEN 1 ELSE 0 END) AS blacklisted
	FROM DomainLinks dl
	JOIN Domains d ON d.domainId = dl.linkId
	LEFT JOIN Whitelist_Domains wl ON wl.domain = d.domain
	LEFT JOIN Blacklist_Domains bl ON bl.domain = d.domain
	WHERE dl.domainId = domainId
	ORDER BY whitelisted DESC, blacklisted, d.domain ASC
END;

$$;

-- File: Stored Procedures/Domains/Services/DomainServices_Add.sql
CREATE OR REPLACE PROCEDURE  public."DomainServices_Add"
(
    IN domainId INT,
    IN serviceIds TEXT
);
LANGUAGE plpgsql
AS $$
BEGIN
    -- Create a temporary table to hold the service IDs
    CREATE TABLE IF NOT EXISTS #ServiceIds
(
    "Id" INT
);
    -- Insert the service IDs into the temporary table
    INSERT INTO #ServiceIds ("Id")
    SELECT CAST(value AS INT) FROM STRING_SPLIT(serviceIds, ',');
    -- Insert new domain-service relationships (avoiding duplicates)
    INSERT INTO public."DomainServices" ("domainId", "serviceId")
    SELECT domainId, si."Id"
    FROM #ServiceIds si
    LEFT JOIN public."DomainServices" ds ON ds."domainId" = domainId AND ds."serviceId" = si."Id"
    WHERE ds."domainId" IS NULL;
    -- Clean up
    DROP TABLE #ServiceIds;
END

$$;

-- File: Stored Procedures/Domains/Services/DomainServices_Filter.sql
CREATE OR REPLACE PROCEDURE  public."DomainServices_Filter"
(
    IN search VARCHAR(100) DEFAULT NULL,
    IN start INT DEFAULT 0,
    IN length INT DEFAULT 50
);
LANGUAGE plpgsql
AS $$
DECLARE
    totalCount INT;
BEGIN
    -- Get the total count for pagination
    SELECT COUNT(*)
FROM public."DomainServiceNames"
    WHERE (search IS NULL OR Name LIKE '%' + search + '%')
INTO totalCount;
    -- Get the paginated results
    SELECT *
    FROM public."DomainServiceNames"
    WHERE (search IS NULL OR Name LIKE '%' + search + '%')
    ORDER BY Name ASC
    OFFSET start ROWS
    FETCH NEXT length ROWS ONLY;
    -- Return the total count as a second result set
    SELECT totalCount AS TotalCount;
END

$$;

-- File: Stored Procedures/Domains/Services/DomainServices_GetByNames.sql
CREATE OR REPLACE PROCEDURE  public."DomainServices_GetByNames"
(
    IN serviceNames TEXT
);
LANGUAGE plpgsql
AS $$
BEGIN
    -- Create a temporary table to hold the service names
    CREATE TABLE IF NOT EXISTS #ServiceNames
(
    "Name" VARCHAR(64)
);
    -- Insert the service names into the temporary table
    INSERT INTO #ServiceNames ("Name")
    SELECT value FROM STRING_SPLIT(serviceNames, ',');
    -- Create any service names that don't exist yet
    INSERT INTO public."DomainServiceNames" ("Name")
    SELECT DISTINCT sn."Name"
    FROM #ServiceNames sn
    LEFT JOIN public."DomainServiceNames" dsn ON dsn."Name" = sn."Name"
    WHERE dsn."Id" IS NULL;
    -- Return the IDs for all service names
    SELECT dsn."Id", dsn."Name"
    FROM public."DomainServiceNames" dsn
    INNER JOIN #ServiceNames sn ON dsn."Name" = sn."Name";
    -- Clean up
    DROP TABLE #ServiceNames;
END

$$;

-- File: Stored Procedures/Domains/Update/Domain_FindDescription.sql
CREATE OR REPLACE PROCEDURE  public."Domain_FindDescription"
(
    IN domainId INT DEFAULT 0
);
LANGUAGE plpgsql
AS $$
DECLARE
    description VARCHAR(255);
BEGIN
SELECT TOP 1 description = summary
	FROM Articles 
	WHERE domainId=domainId
	AND summary != ''
	ORDER BY LEN(url) ASC
	UPDATE Domains SET "description" = description, dateupdated = CURRENT_TIMESTAMP
	WHERE domainId=domainId
	SELECT description
END;

$$;

-- File: Stored Procedures/Domains/Update/Domain_FindTitle.sql
CREATE OR REPLACE PROCEDURE  public."Domain_FindTitle"
(
    IN domainId INT DEFAULT 0
);
LANGUAGE plpgsql
AS $$
DECLARE
    articles TABLE (;
    words TABLE (;
    count INT := 0;
    exclude TABLE(value VARCHAR(32));
    cursor CURSOR, title VARCHAR(250);
    i INT := 0;
    domain VARCHAR(64), domainpart VARCHAR(64), domainpart2 VARCHAR(64);
    domainparts TABLE (value VARCHAR(64));
    domainTitle VARCHAR(128);
BEGIN
--get common word found in all article titles
		title VARCHAR(250)
	);
		word TEXT
	);
	INSERT INTO articles 
	SELECT top 100 a.title FROM Articles a
	WHERE a.domainId = domainId
	SELECT count = COUNT(*) FROM articles
	INSERT INTO exclude ("value")
	VALUES ('and'), ('or'), ('&'), ('the'), ('for'), ('with')
	SET cursor = CURSOR FOR
	SELECT title FROM articles
	OPEN cursor
	FETCH NEXT FROM cursor INTO title
	WHILE @@FETCH_STATUS = 0 BEGIN
		--get all words & phrases from the title
		INSERT INTO words SELECT TRIM(value) FROM (SELECT * FROM STRING_SPLIT(title, ' ')) as tbl WHERE LEN(value) > 2 AND "value" NOT IN (SELECT * FROM exclude)
		INSERT INTO words SELECT TRIM(value) FROM (SELECT * FROM STRING_SPLIT(title, '-')) as tbl WHERE LEN(value) > 2 AND "value" NOT IN (SELECT * FROM exclude)
		INSERT INTO words SELECT TRIM(value) FROM (SELECT * FROM STRING_SPLIT(title, '|')) as tbl WHERE LEN(value) > 2 AND "value" NOT IN (SELECT * FROM exclude)
		INSERT INTO words SELECT TRIM(value) FROM (SELECT * FROM STRING_SPLIT(title, ':')) as tbl WHERE LEN(value) > 2 AND "value" NOT IN (SELECT * FROM exclude)
		INSERT INTO words SELECT TRIM(value) FROM (SELECT * FROM STRING_SPLIT(title, ';')) as tbl WHERE LEN(value) > 2 AND "value" NOT IN (SELECT * FROM exclude)
		INSERT INTO words SELECT TRIM(value) FROM (SELECT * FROM STRING_SPLIT(title, '/')) as tbl WHERE LEN(value) > 2 AND "value" NOT IN (SELECT * FROM exclude)
		FETCH NEXT FROM cursor INTO title
	END
	CLOSE cursor
	DEALLOCATE cursor
	SELECT count = COUNT(*) FROM words
	--get count of all duplicate words & phrases
	SELECT domain = domain FROM Domains WHERE domainId=domainId
	INSERT INTO domainparts SELECT * FROM STRING_SPLIT(domain, '.')
	SELECT TOP 1 domainpart = REPLACE("value", '-', '%') FROM domainparts
	SELECT domainpart2 = STRING_AGG("value", '') FROM domainparts
	--PRINT domainpart2
	SELECT TOP 1 domainTitle = TRIM(word)
	FROM (
		SELECT b.score, w.word, COUNT(w.word) AS total, LEN(w.word) AS "length"
		FROM words w
		CROSS APPLY (
			SELECT CASE 
			WHEN PATINDEX(domainpart + '%', REPLACE(w.word, ' ', '')) > 0 THEN 50 
			WHEN PATINDEX(domainpart2 + '%', REPLACE(w.word, ' ', '')) > 0 THEN 100
			ELSE 0 END AS score
		) AS b
		GROUP BY w.word, b.score
		HAVING COUNT(w.word) > 1
	) AS tbl
	ORDER BY score DESC, total DESC, "length" DESC
	UPDATE Domains SET title=domainTitle, hastitle=1, dateupdated = CURRENT_TIMESTAMP WHERE domainId=domainId
	SELECT domainTitle
END;

$$;

-- File: Stored Procedures/Domains/Update/Domain_HasFreeContent.sql
CREATE OR REPLACE PROCEDURE  public."Domain_HasFreeContent"
(
    IN domainId INT,
    IN free BOOLEAN DEFAULT FALSE
);
LANGUAGE plpgsql
AS $$
BEGIN
UPDATE "Domains" SET free=free, dateupdated = CURRENT_TIMESTAMP WHERE domainId=domainId
END;

$$;

-- File: Stored Procedures/Domains/Update/Domain_IsDeleted.sql
CREATE OR REPLACE PROCEDURE  public."Domain_IsDeleted"
(
    IN domainId INT,
    IN delete BOOLEAN DEFAULT TRUE
);
LANGUAGE plpgsql
AS $$
BEGIN
UPDATE "Domains" SET "deleted"=delete, dateupdated = CURRENT_TIMESTAMP WHERE domainId=domainId
END;

$$;

-- File: Stored Procedures/Domains/Update/Domain_IsEmpty.sql
CREATE OR REPLACE PROCEDURE  public."Domain_IsEmpty"
(
    IN domainId INT,
    IN empty BOOLEAN DEFAULT FALSE
);
LANGUAGE plpgsql
AS $$
BEGIN
UPDATE "Domains" SET "empty"=empty, dateupdated = CURRENT_TIMESTAMP WHERE domainId=domainId
END;

$$;

-- File: Stored Procedures/Domains/Update/Domain_RequireSubscription.sql
CREATE OR REPLACE PROCEDURE  public."Domain_RequireSubscription"
(
    IN domainId INT,
    IN required BOOLEAN DEFAULT FALSE
);
LANGUAGE plpgsql
AS $$
BEGIN
UPDATE "Domains" SET paywall=required, dateupdated = CURRENT_TIMESTAMP WHERE domainId=domainId
END;

$$;

-- File: Stored Procedures/Domains/Update/Domain_UpdateHttpsWww.sql
CREATE OR REPLACE PROCEDURE  public."Domain_UpdateHttpsWww"
(
    IN domainId INT DEFAULT 0,
    IN https BOOLEAN DEFAULT FALSE,
    IN www BOOLEAN DEFAULT FALSE
);
LANGUAGE plpgsql
AS $$
BEGIN
UPDATE Domains SET "https" = https, "www" = www, dateupdated = CURRENT_TIMESTAMP
	WHERE domainId=domainId
END;

$$;

-- File: Stored Procedures/Domains/Update/Domain_UpdateInfo.sql
CREATE OR REPLACE PROCEDURE  public."Domain_UpdateInfo"
(
    IN domainId INT DEFAULT 0,
    IN title VARCHAR(128),
    IN description VARCHAR(255),
    IN lang char(2) DEFAULT 'en'
);
LANGUAGE plpgsql
AS $$
BEGIN
UPDATE Domains SET "title"=title, "description" = description, lang=lang, hastitle=1, dateupdated = CURRENT_TIMESTAMP
	WHERE domainId=domainId
END;

$$;

-- File: Stored Procedures/Domains/Update/Domain_UpdateLanguage.sql
CREATE OR REPLACE PROCEDURE  public."Domain_UpdateLanguage"
(
    IN domainId INT DEFAULT 0,
    IN lang varchar(6)
);
LANGUAGE plpgsql
AS $$
BEGIN
UPDATE Domains SET lang = lang, dateupdated = CURRENT_TIMESTAMP
	WHERE domainId=domainId
END;

$$;

-- File: Stored Procedures/Domains/Update/Domain_UpdateType.sql
CREATE OR REPLACE PROCEDURE  public."Domain_UpdateType"
(
    IN domainId INT DEFAULT 0,
    IN type INT DEFAULT -1
);
LANGUAGE plpgsql
AS $$
BEGIN
UPDATE Domains SET "type" = type, dateupdated = CURRENT_TIMESTAMP
	WHERE domainId=domainId
END;

$$;

-- File: Stored Procedures/Domains/Update/Domain_UpdateType2.sql
CREATE OR REPLACE PROCEDURE  public."Domain_UpdateType2"
(
    IN domainId INT DEFAULT 0,
    IN type INT DEFAULT -1
);
LANGUAGE plpgsql
AS $$
BEGIN
UPDATE Domains SET "type2" = type, dateupdated = CURRENT_TIMESTAMP
	WHERE domainId=domainId
END;

$$;

-- File: Stored Procedures/Downloads/Download_Delete.sql
CREATE OR REPLACE PROCEDURE  public."Download_Delete"
(
    IN qid bigint DEFAULT 0
);
LANGUAGE plpgsql
AS $$
DECLARE
    url VARCHAR(250), domainId INT;
BEGIN
SELECT url = "url", domainId=domainId FROM DownloadQueue WHERE qid=qid
	--delete the article associated with download
	DELETE FROM Articles WHERE "url" = (SELECT "url" FROM DownloadQueue WHERE qid=qid)
	DELETE FROM DownloadQueue WHERE qid=qid
	DELETE FROM Downloads WHERE id=qid
	UPDATE Domains SET inqueue-=1 WHERE domainId=domainId
END;

$$;

-- File: Stored Procedures/Downloads/Download_Update.sql
CREATE OR REPLACE PROCEDURE  public."Download_Update"
(
    IN qid bigint DEFAULT 0,
    IN status INT DEFAULT 0
);
LANGUAGE plpgsql
AS $$
BEGIN
UPDATE DownloadQueue SET status=status WHERE qid=qid
END;

$$;

-- File: Stored Procedures/Downloads/Download_UpdateType.sql
CREATE OR REPLACE PROCEDURE  public."Download_UpdateType"
(
    IN qId BIGINT,
    IN type SMALLINT
);
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public."DownloadQueue"
    SET "type" = type
    WHERE "qid" = qId
END

$$;

-- File: Stored Procedures/Downloads/Download_UpdateUrl.sql
CREATE OR REPLACE PROCEDURE  public."Download_UpdateUrl"
(
    IN qId bigint DEFAULT 0,
    IN url VARCHAR(250),
    IN domain VARCHAR(250)
);
LANGUAGE plpgsql
AS $$
DECLARE
    domainId INT;
BEGIN
SELECT domainId=domainId FROM Domains WHERE domain=domain
	IF domainId IS NULL BEGIN
		SET domainId = nextval('public."SequenceDomains"')
		INSERT INTO Domains (domainId, domain) VALUES (domainId, domain)
	END
	IF EXISTS(SELECT * FROM DownloadQueue WHERE url=url) BEGIN
		--remove existing download queue item
		DELETE FROM DownloadQueue WHERE url=url
	END
	UPDATE DownloadQueue SET "url"=url, domainId=domainId WHERE qid=qid
	IF EXISTS(SELECT * FROM Downloads WHERE url=url) BEGIN
		DELETE FROM Downloads WHERE url=url
	END
	UPDATE Downloads SET "url"=url, domainId=domainId WHERE id=qid
END;

$$;

-- File: Stored Procedures/Downloads/DownloadQueue_Add.sql
CREATE OR REPLACE PROCEDURE  public."DownloadQueue_Add"
(
    IN url TEXT DEFAULT '',
    IN domain VARCHAR(64) DEFAULT '',
    IN parentId INT,
    IN feedId INT DEFAULT 0
);
LANGUAGE plpgsql
AS $$
DECLARE
    domainId INT, qid BIGINT, count INT := 0, title VARCHAR(128);
    title_results TABLE (title TEXT);
    domain_results TABLE (id INT);
BEGIN
IF EXISTS(SELECT * FROM Domains WHERE domain=domain) BEGIN
	--get domain ID
	SELECT domainId = domainId, title = title FROM Domains WHERE domain=domain
	IF title = '' BEGIN
		IF (SELECT COUNT(*) FROM Articles WHERE domainId=domainId) >= 10 BEGIN
			--get common word found in all article titles
			INSERT INTO title_results
			EXEC Domain_FindTitle domainId=domainId
		END
	END
	IF parentId > 0 AND parentId <> domainId BEGIN
		EXEC DomainLink_Add domainId=parentId, linkId=domainId
	END
END ELSE BEGIN
	--create domain ID
	INSERT INTO domain_results
	EXEC Domain_Add domain=domain, parentId=parentId
	SELECT domainId = domainId, title = title FROM Domains WHERE domain=domain
END
	IF NOT EXISTS(SELECT * FROM DownloadQueue WHERE url=url) 
	AND NOT EXISTS(SELECT * FROM Downloads WHERE url=url) BEGIN
		SET qid = nextval('public."SequenceDownloadQueue"')
		INSERT INTO DownloadQueue (qid, "url", "path", feedId, domainId, "status", datecreated) 
		VALUES (qid, url, public.GetPathFromUrl(url, domain), feedId, domainId, 0, CURRENT_TIMESTAMP)
		UPDATE Domains SET inqueue+=1 WHERE domainId=domainId
	END ELSE BEGIN
		SELECT qid = qid FROM DownloadQueue WHERE url=url
		IF qid IS NULL BEGIN
			SELECT qid = id FROM Downloads WHERE url=url
		END
	END
	SELECT qid AS qid
END;

$$;

-- File: Stored Procedures/Downloads/DownloadQueue_Archive.sql
CREATE OR REPLACE PROCEDURE  public."DownloadQueue_Archive"
(
    IN qid bigint DEFAULT 0
);
LANGUAGE plpgsql
AS $$
DECLARE
    domainId INT;
BEGIN
SELECT domainId=domainId FROM DownloadQueue WHERE qid=qid
	UPDATE DownloadQueue SET status=2 WHERE qid=qid
	UPDATE Domains SET inqueue-=1 WHERE domainId=domainId
END;

$$;

-- File: Stored Procedures/Downloads/DownloadQueue_BulkAdd.sql
CREATE OR REPLACE PROCEDURE  public."DownloadQueue_BulkAdd"
(
    IN urls TEXT DEFAULT '', --comma delimited list,
    IN domain VARCHAR(64) DEFAULT '',
    IN parentId INT,
    IN feedId INT DEFAULT 0
);
LANGUAGE plpgsql
AS $$
DECLARE
    cursor CURSOR, url TEXT, domainId INT, qid BIGINT, count INT := 0, title VARCHAR(128);
    title_results TABLE (title TEXT);
    domain_results TABLE (id INT);
BEGIN
SELECT * INTO #urls FROM public.SplitArray(urls, ',')
IF EXISTS(SELECT * FROM Domains WHERE domain=domain) BEGIN
	--get domain ID
	SELECT domainId = domainId, title = title FROM Domains WHERE domain=domain
	IF title = '' BEGIN
		IF (SELECT COUNT(*) FROM Articles WHERE domainId=domainId) >= 10 BEGIN
			--get common word found in all article titles
			INSERT INTO title_results
			EXEC Domain_FindTitle domainId=domainId
		END
	END
	IF parentId > 0 AND parentId <> domainId BEGIN
		EXEC DomainLink_Add domainId=parentId, linkId=domainId
	END
END ELSE BEGIN
	--create domain ID
	INSERT INTO domain_results
	EXEC Domain_Add domain=domain, parentId=parentId
	SELECT domainId = domainId, title = title FROM Domains WHERE domain=domain
END
SET cursor = CURSOR FOR
SELECT DISTINCT "value" FROM #urls
OPEN cursor
FETCH NEXT FROM cursor INTO url
WHILE @@FETCH_STATUS = 0 BEGIN
	IF NOT EXISTS(SELECT * FROM DownloadQueue WHERE url=url) 
	AND NOT EXISTS(SELECT * FROM Downloads WHERE url=url)
	AND NOT EXISTS(SELECT * FROM Articles WHERE url=url) BEGIN
		SET qid = nextval('public."SequenceDownloadQueue"')
		INSERT INTO DownloadQueue (qid, "url", "path", feedId, domainId, "status", datecreated) 
		VALUES (qid, url, public.GetPathFromUrl(url, domain), feedId, domainId, 0, CURRENT_TIMESTAMP)
		SET count = count + 1
	END
	FETCH NEXT FROM cursor INTO url
END
CLOSE cursor
DEALLOCATE cursor
UPDATE Domains SET inqueue+=count WHERE domainId=domainId
SELECT count
END;

$$;

-- File: Stored Procedures/Downloads/DownloadQueue_Check.sql
CREATE OR REPLACE PROCEDURE  public."DownloadQueue_Check"
(
    IN domaindelay INT DEFAULT 60, -- in seconds,
    IN domain VARCHAR(64) DEFAULT '',
    IN feedId INT DEFAULT 0,
    IN sort INT DEFAULT 0, -- 0 = newest, 1 = oldest, 2 = domain-level, 3 = random,
    IN qid bigint DEFAULT 0
);
LANGUAGE plpgsql
AS $$
DECLARE
    domainId INT, maxQid bigint := 0, randQid bigint;
    checkedDomains TABLE (domainId INT);
BEGIN
--BEGIN TRANSACTION
	IF qid = 0 BEGIN
		IF sort = 2 OR sort = 3 BEGIN -- random queue item
			SELECT maxQid = MAX(qid) FROM DownloadQueue
			SET randQid = CONVERT(bigint, (RAND() * maxQid))
		END
		IF domain IS NOT NULL AND domain <> '' BEGIN
			SELECT domainId = domainId FROM Domains WHERE domain=domain
		END
		INSERT INTO checkedDomains
		SELECT domainId FROM Domains
		WHERE lastchecked >= DATEADD(SECOND, 0 - domaindelay, CURRENT_TIMESTAMP)
		SELECT TOP 1 qid = q.qid, domainId = q.domainId
		FROM DownloadQueue q --
		JOIN Domains d ON d.domainId = q.domainId
		LEFT JOIN Whitelist_Domains w ON w.domain = d.domain -- must be a whitelisted domain
		LEFT JOIN Blacklist_Domains b ON b.domain = d.domain -- check for blacklisted domain
		WHERE
		(
			-- filter by domain name
			(domain IS NOT NULL AND domain <> '' AND q.domainId = domainId)
			OR domain IS NULL OR domain = ''
		);
		-- filter domains that have not been checked recently
		AND q.domainId NOT IN (SELECT domainId FROM checkedDomains)
		AND (
			-- filter by feed
			(feedId > 0 AND q.feedId = feedId)
			OR feedId <= 0
		);
		-- filter domains that are not behind a paywall
		AND (d.paywall = 0 OR (d.paywall = 1 AND d.free = 1))
		AND ( 
			-- get random download queue item
			((sort = 2 OR sort = 3) AND maxQid > 0 AND q.qid >= randQid)
			OR maxQid = 0
		); 
		AND (
			-- get download queue item that only contains domain name (domain home page)
			(sort = 2 AND LEN(q."url") <= LEN(d.domain) + 11)
			OR sort != 2
		);
		AND (
			-- filter by whitelisted domains only (unless we're getting domain home pages)
			(sort != 2 AND w.domain IS NOT NULL)
			OR sort = 2
		);
		-- filter all blacklisted domains
		AND b.domain IS NULL
		AND q.status = 0
		AND (d.lang = '' OR d.lang = 'en')
		ORDER BY 
		CASE WHEN sort = 0 THEN q.datecreated END DESC
	END ELSE BEGIN
		SELECT domainId = domainId FROM DownloadQueue WHERE qid = qid
		IF domainId IS NULL RETURN --exit sproc if we fail to get domainId
	END
	IF qid > 0 BEGIN
		--WAITFOR DELAY '00:00:03' -- for debugging transactions
		UPDATE DownloadQueue SET status=1 WHERE qid=qid
		UPDATE Domains SET lastchecked = CURRENT_TIMESTAMP
		WHERE domainId = domainId
		-- get next download in the queue
		SELECT q.*, d.domain, d.articles
		FROM DownloadQueue q 
		JOIN Domains d ON d.domainId = q.domainId
		WHERE qid=qid
		-- get list of download rules for domain that queue item belongs to
		SELECT * FROM DownloadRules WHERE domainId = (SELECT domainId FROM DownloadQueue q WHERE qid=qid)
	END
	--COMMIT
END;

$$;

-- File: Stored Procedures/Downloads/DownloadQueue_Move.sql
CREATE OR REPLACE PROCEDURE  public."DownloadQueue_Move"
(
    IN qid bigint DEFAULT 0
);
LANGUAGE plpgsql
AS $$
BEGIN
--move related Download Queue record into Downloads table
	INSERT INTO Downloads ("id", "feedId", "domainId", "status", "type", "tries", "url", "path", "datecreated") 
	SELECT * FROM DownloadQueue WHERE qid=qid
	DELETE FROM DownloadQueue WHERE qid=qid
END;

$$;

-- File: Stored Procedures/Downloads/DownloadQueue_MoveArchived.sql
CREATE OR REPLACE PROCEDURE public."DownloadQueue_MoveArchived"
LANGUAGE plpgsql
AS $$
BEGIN
INSERT INTO Downloads ("id", "feedId", "domainId", "type", "status", "tries", "url", "path", "datecreated") 
	SELECT * FROM DownloadQueue WHERE "status"=2 AND NOT EXISTS(SELECT * FROM Downloads WHERE id=qid)
	DELETE FROM DownloadQueue WHERE "status"=2
END;

$$;

-- File: Stored Procedures/Downloads/Downloads_GetCount.sql
CREATE OR REPLACE PROCEDURE public."Downloads_GetCount"
LANGUAGE plpgsql
AS $$
BEGIN
SELECT COUNT(*) FROM Downloads
END;

$$;

-- File: Stored Procedures/Feeds/Feed_Add.sql
CREATE OR REPLACE PROCEDURE  public."Feed_Add"
(
    IN doctype INT DEFAULT 1,
    IN categoryId INT,
    IN title VARCHAR(100) DEFAULT '',
    IN url VARCHAR(100) DEFAULT '',
    IN domain VARCHAR(64) DEFAULT '',
    IN filter TEXT DEFAULT '',
    IN checkIntervals INT DEFAULT 720 --(12 hours)
);
LANGUAGE plpgsql
AS $$
DECLARE
    domainId INT;
    feedId INT := nextval('public."SequenceFeeds"');
BEGIN
IF NOT EXISTS(SELECT * FROM Domains WHERE domain=domain) BEGIN
	--get domain ID
	SELECT domainId = domainId, title = title FROM Domains WHERE domain=domain
END ELSE BEGIN
	--create domain ID
	SET domainId = nextval('public."SequenceDomains"')
	INSERT INTO Domains (domainId, parentId, domain, lastchecked) VALUES (domainId, 0, domain, DATEADD(HOUR, -1, CURRENT_TIMESTAMP))
END
INSERT INTO Feeds (feedId, domainId, doctype, categoryId, title, url, checkIntervals, filter, lastChecked) 
VALUES (feedId, domainId, doctype, categoryId, title, url, checkIntervals, filter, DATEADD(HOUR, -24, CURRENT_TIMESTAMP))
BEGIN TRY
	INSERT INTO Whitelist_Domains (domain) VALUES (domain)
END TRY
BEGIN CATCH
END CATCH
SELECT feedId
END;

$$;

-- File: Stored Procedures/Feeds/Feed_AddCheckedLog.sql
CREATE OR REPLACE PROCEDURE  public."FeedCheckedLog_Add"
(
    IN feedId INT DEFAULT 0,
    IN links INT DEFAULT 0
);
LANGUAGE plpgsql
AS $$
BEGIN
INSERT INTO FeedsCheckedLog (feedId, links, datechecked)
	VALUES (feedId, links, CURRENT_TIMESTAMP)
	UPDATE Feeds SET lastChecked = CURRENT_TIMESTAMP
END;

$$;

-- File: Stored Procedures/Feeds/Feed_Checked.sql
CREATE OR REPLACE PROCEDURE  public."Feed_Checked"
(
    IN feedId INT DEFAULT 0
);
LANGUAGE plpgsql
AS $$
BEGIN
UPDATE Feeds SET lastChecked=CURRENT_TIMESTAMP WHERE feedId=feedId
RETURN 0
END;

$$;

-- File: Stored Procedures/Feeds/Feed_GetInfo.sql
CREATE OR REPLACE PROCEDURE  public."Feed_GetInfo"
(
    IN feedId INT
);
LANGUAGE plpgsql
AS $$
BEGIN
SELECT * FROM Feeds WHERE feedId=feedId
END;

$$;

-- File: Stored Procedures/Feeds/Feeds_Categories_GetList.sql
CREATE OR REPLACE PROCEDURE public."Feeds_Categories_GetList"
LANGUAGE plpgsql
AS $$
BEGIN
SELECT * FROM FeedCategories ORDER BY title ASC
END;

$$;

-- File: Stored Procedures/Feeds/Feeds_Category_Add.sql
CREATE OR REPLACE PROCEDURE  public."Feeds_Category_Add"
(
    IN title VARCHAR(64)
);
LANGUAGE plpgsql
AS $$
DECLARE
    id INT := nextval('public."SequenceFeedCategories"');
BEGIN
INSERT INTO FeedCategories (categoryId, title) VALUES (id, title)
END;

$$;

-- File: Stored Procedures/Feeds/Feeds_Check.sql
CREATE OR REPLACE PROCEDURE  public."Feeds_Check"
(
    IN feedId INT DEFAULT 0
);
LANGUAGE plpgsql
AS $$
BEGIN
SELECT f.*, c.title AS category
	FROM Feeds f 
	JOIN FeedCategories c ON c.categoryId = f.categoryId
	WHERE f.lastChecked < DATEADD(MINUTE, -1 * f.checkIntervals, CURRENT_TIMESTAMP)
	AND (
		(feedId > 0 AND f.feedId = feedId)
		OR feedId = 0
	);
END;

$$;

-- File: Stored Procedures/Feeds/Feeds_Filter.sql
CREATE OR REPLACE PROCEDURE  public."Feeds_Filter"
(
    IN Start INT,
    IN Length INT,
    IN Search VARCHAR(255),
    IN Sort INT
);
LANGUAGE plpgsql
AS $$
BEGIN
    SELECT *
    FROM Feeds
    WHERE Title LIKE '%' + Search + '%' OR Url LIKE '%' + Search + '%'
    ORDER BY 
        CASE WHEN Sort = 0 THEN Title END ASC,
        CASE WHEN Sort = 1 THEN Title END DESC,
        CASE WHEN Sort = 2 THEN Url END ASC,
        CASE WHEN Sort = 3 THEN Url END DESC,
        CASE WHEN Sort = 4 THEN CAST(CheckIntervals AS BIGINT) END ASC,
        CASE WHEN Sort = 5 THEN CAST(CheckIntervals AS BIGINT) END DESC
    OFFSET Start ROWS FETCH NEXT Length ROWS ONLY;
END

$$;

-- File: Stored Procedures/Feeds/Feeds_GetList.sql
CREATE OR REPLACE PROCEDURE public."Feeds_GetList"
LANGUAGE plpgsql
AS $$
BEGIN
SELECT f.*, fc.title AS category
FROM Feeds f
JOIN FeedCategories fc ON fc.categoryId = f.categoryId
WHERE feedId > 0 ORDER BY fc.title ASC, f.title ASC
END;

$$;

-- File: Stored Procedures/Feeds/Feeds_GetListWithLogs.sql
CREATE OR REPLACE PROCEDURE  public."Feeds_GetListWithLogs"
(
    IN days INT DEFAULT 7,
    IN dateStart date
);
LANGUAGE plpgsql
AS $$
DECLARE
    tblresults TABLE (;
BEGIN
DECLARE 
	cursor1 CURSOR,
	cursor2 CURSOR,
	feedId INT,
	title VARCHAR(100),
	url VARCHAR(100),
	checkIntervals INT = 720,
	lastChecked TIMESTAMP,
	filter TEXT,
	logfeedId INT,
	loglinks smallint,
	logdatechecked TIMESTAMP
		feedId INT NOT NULL,
		title VARCHAR(100) NULL,
		url VARCHAR(100) NULL,
		checkIntervals INT,
		lastChecked TIMESTAMP NULL,
		filter TEXT NULL,
		loglinks smallint NULL,
		logdatechecked TIMESTAMP NULL
	);
	SET cursor1 = CURSOR FOR 
	SELECT * FROM feeds WHERE feedId > 0 ORDER BY checkIntervals ASC, title ASC
	OPEN cursor1
	FETCH FROM cursor1 INTO
	feedId, title, url, checkIntervals, lastChecked, filter
	WHILE @@FETCH_STATUS = 0 BEGIN
		/*add feed to results table */
		INSERT INTO tblresults (feedId, title, url, checkIntervals, lastChecked, filter)
		VALUES (feedId, title, url, checkIntervals, lastChecked, filter)
		/* get log data for each feed */
		SET cursor2 = CURSOR FOR 
		SELECT * FROM FeedsCheckedLog 
		WHERE feedId=feedId 
		AND datechecked >= dateStart
		AND datechecked <= DATEADD(DAY, days, dateStart)
		ORDER BY datechecked ASC
		OPEN cursor2
		FETCH FROM cursor2 INTO
		logfeedId, loglinks, logdatechecked
		WHILE @@FETCH_STATUS = 0 BEGIN
			/* add feed log record to results table */
			INSERT INTO tblresults (feedId, loglinks, logdatechecked)
			VALUES(feedId, loglinks, logdatechecked)
			FETCH FROM cursor2 INTO
			logfeedId, loglinks, logdatechecked
		END
		CLOSE cursor2
		DEALLOCATE cursor2
		FETCH FROM cursor1 INTO
		feedId, title, url, checkIntervals, lastChecked, filter
	END
	CLOSE cursor1
	DEALLOCATE cursor1
	/* finally, return results */
	SELECT * FROM tblresults
END;

$$;

-- File: Stored Procedures/ResetAllSequences.sql
CREATE OR REPLACE PROCEDURE public."ResetAllSequences"
LANGUAGE plpgsql
AS $$
DECLARE
    SequenceName VARCHAR(128);
    TableName VARCHAR(128);
    MaxId INT;
    IdColumnName VARCHAR(128);
    SQL TEXT;
    sequence_cursor CURSOR FOR;
BEGIN
    -- Create a temporary table to hold sequence-to-table mappings
    BEGIN TRY
        IF OBJECT_ID('tempdb..#SequenceMappings') IS NOT NULL
        BEGIN
            DROP TABLE #SequenceMappings
        END
    END TRY
    BEGIN CATCH
        PRINT 'Error: ' + ERROR_MESSAGE()
    END CATCH
    CREATE TABLE IF NOT EXISTS #SequenceMappings
(
    "SequenceName" VARCHAR(128),
    "TableName" VARCHAR(128),
    "IdColumnName" VARCHAR(128)
);-- Insert all sequence-to-table mappings
    INSERT INTO #SequenceMappings (SequenceName, TableName, IdColumnName) VALUES
        ('SequenceAnalyzerRules', 'AnalyzerRules', 'ruleId'),
        ('SequenceArticleBugs', 'ArticleBugs', 'bugId'),
        ('SequenceArticles', 'Articles', 'articleId'),
        ('SequenceDomainCollectionGroups', 'DomainCollectionGroups', 'colgroupId'),
        ('SequenceDomainCollections', 'DomainCollections', 'colId'),
        ('SequenceDomainTypeMatches', 'DomainTypeMatches', 'matchId'),
        ('SequenceDomains', 'Domains', 'domainId'),
        ('SequenceDownloadQueue', 'DownloadQueue', 'qid'),
        ('SequenceDownloadRules', 'DownloadRules', 'ruleId'),
        ('SequenceFeedCategories', 'FeedCategories', 'categoryId'),
        ('SequenceFeeds', 'Feeds', 'feedId'),
        ('SequenceJournalCategories', 'JournalCategories', 'Id'),
        ('SequenceJournalCheckListItems', 'JournalCheckListItems', 'Id'),
        ('SequenceJournalCheckLists', 'JournalCheckLists', 'Id'),
        ('SequenceJournalEntrySnapshots', 'JournalEntrySnapshots', 'Id'),
        ('SequenceJournalFiles', 'JournalFiles', 'Id'),
        ('SequenceJournalImages', 'JournalImages', 'Id'),
        ('SequenceJournalVideos', 'JournalVideos', 'Id'),
        ('SequenceJournals', 'Journals', 'Id'),
        ('SequenceStatisticsProjects', 'StatisticsProjects', 'projectId'),
        ('SequenceStatisticsResults', 'StatisticsResults', 'statId'),
        ('SequenceSubjects', 'Subjects', 'subjectId'),
        ('SequenceWords', 'Words', 'wordId')
    -- Cursor to iterate through all sequences
    SELECT SequenceName, TableName, IdColumnName FROM #SequenceMappings
    OPEN sequence_cursor
    FETCH NEXT FROM sequence_cursor INTO SequenceName, TableName, IdColumnName
    WHILE @@FETCH_STATUS = 0
    BEGIN
        BEGIN TRY
            -- Build dynamic SQL to get MAX(Id) from the table
            SET SQL = N'SELECT MaxIdOut = ISNULL(MAX(' + IdColumnName + '), 0) FROM public.[' + TableName + ']'
            -- Execute dynamic SQL to get the max ID
            EXEC sp_executesql SQL, N'@MaxIdOut INT OUTPUT', MaxIdOut = MaxId OUTPUT
            -- Reset the sequence to MAX(Id) + 1
            IF MaxId > 0
            BEGIN
                SET SQL = N'ALTER SEQUENCE public.[' + SequenceName + '] RESTART WITH ' + CAST(MaxId + 1 AS VARCHAR(20))
                EXEC sp_executesql SQL
                PRINT 'Reset ' + SequenceName + ' to ' + CAST(MaxId + 1 AS VARCHAR(20))
            END
            ELSE
            BEGIN
                -- If table is empty, reset to 1
                SET SQL = N'ALTER SEQUENCE public.[' + SequenceName + '] RESTART WITH 1'
                EXEC sp_executesql SQL
                PRINT 'Reset ' + SequenceName + ' to 1 (table is empty)'
            END
        END TRY
        BEGIN CATCH
            PRINT 'Error resetting ' + SequenceName + ': ' + ERROR_MESSAGE()
        END CATCH
        FETCH NEXT FROM sequence_cursor INTO SequenceName, TableName, IdColumnName
    END
    CLOSE sequence_cursor
    DEALLOCATE sequence_cursor
    -- Clean up
    DROP TABLE #SequenceMappings
    PRINT 'All sequences have been reset successfully!'
END

$$;

-- File: Stored Procedures/Subjects/Subject_Create.sql
CREATE OR REPLACE PROCEDURE  public."Subject_Create"
(
    IN parentId INT DEFAULT 0,
    IN grammartype INT DEFAULT 0,
    IN score INT DEFAULT 0,
    IN title VARCHAR(50),
    IN breadcrumb TEXT DEFAULT ''
);
LANGUAGE plpgsql
AS $$
DECLARE
    create BOOLEAN := 1, hierarchy VARCHAR(50) = '';
    id INT := nextval('public."SequenceSubjects"');
BEGIN
IF parentId > 0 BEGIN
		IF (SELECT COUNT(*) FROM Subjects WHERE breadcrumb = breadcrumb AND title=title) > 0 BEGIN
			/* subject already exists */
			SET create = 0
		END ELSE BEGIN
			/* get hierarchy indexes */
			SELECT hierarchy = hierarchy FROM Subjects WHERE subjectId=parentId
			if hierarchy <> '' BEGIN
			 SET hierarchy = hierarchy  + '>' + CONVERT(VARCHAR(10),parentId)
			END ELSE BEGIN
			 SET hierarchy =  CONVERT(VARCHAR(10),parentId)
			END
		END
	END ELSE BEGIN
		IF (SELECT COUNT(*) FROM Subjects WHERE parentId=0 AND title=title) > 0 BEGIN
			/* root subject already exists */
			SET create = 0
		END
	END
	IF create = 1 BEGIN
		/* finally, create subject */
		INSERT INTO Subjects (subjectId, parentId, grammartype, score, title, breadcrumb, hierarchy)
		VALUES (id, parentId, grammartype, score, title, breadcrumb, hierarchy)
		SELECT id
	END ELSE BEGIN
		SELECT 0
	END
END;

$$;

-- File: Stored Procedures/Subjects/Subject_GetById.sql
CREATE OR REPLACE PROCEDURE  public."Subject_GetById"
(
    IN subjectId INT
);
LANGUAGE plpgsql
AS $$
BEGIN
SELECT * FROM Subjects WHERE subjectId=subjectId
END;

$$;

-- File: Stored Procedures/Subjects/Subject_GetByTitle.sql
CREATE OR REPLACE PROCEDURE  public."Subject_GetByTitle"
(
    IN title VARCHAR(50),
    IN breadcrumb TEXT
);
LANGUAGE plpgsql
AS $$
BEGIN
SELECT * FROM Subjects WHERE breadcrumb = breadcrumb AND title=title
END;

$$;

-- File: Stored Procedures/Subjects/Subject_Move.sql
CREATE OR REPLACE PROCEDURE  public."Subject_Move"
(
    IN subjectId INT DEFAULT 1,
    IN newParent INT DEFAULT 127
);
LANGUAGE plpgsql
AS $$
BEGIN
DECLARE 
	title VARCHAR(50) = '',
	bread VARCHAR(500) = '', 
	hier VARCHAR(50), 
	newBread VARCHAR(500) = '',
	newHier VARCHAR(50),
	newTitle VARCHAR(50),
	cursor1 CURSOR,
	childId INT, parentId INT,
	parentTitle VARCHAR(50),
	parentHier VARCHAR(50),
	parentBread VARCHAR(500)
	/* get breadcrumb info */
	SELECT bread = breadcrumb, hier = hierarchy FROM Subjects WHERE subjectId=subjectId
	IF bread <> '' BEGIN
		SET bread = bread + '>' + title
		SET hier = hier + '>' + CONVERT(VARCHAR(25),subjectId)
	END ELSE BEGIN
		SET bread = title
		SET hier = CONVERT(VARCHAR(25),subjectId)
	END
	SELECT newBread = breadcrumb, newHier = hierarchy, newTitle=title FROM Subjects WHERE subjectId=newParent
	IF newBread <> '' BEGIN
		SET newBread = newBread + '>' + newTitle
		SET newHier = newHier + '>' + CONVERT(VARCHAR(25),newParent)
	END ELSE BEGIN
		SET newBread = newTitle
		SET newHier = CONVERT(VARCHAR(25),newParent)
	END
	/* update subject */
	UPDATE Subjects 
	SET parentId=newParent, hierarchy=newHier, breadcrumb=newBread 
	WHERE subjectId=subjectId
	/* update each child subject */
	SET cursor1 = CURSOR FOR
	SELECT subjectId, parentId FROM Subjects WHERE hierarchy LIKE hier + '>%' OR hierarchy = hier ORDER BY hierarchy ASC
	OPEN cursor1
	FETCH FROM cursor1 INTO
	childId, parentId
	WHILE @@FETCH_STATUS = 0
    BEGIN
		SELECT parentTitle = title, parentHier=hierarchy, parentBread=breadcrumb FROM Subjects WHERE subjectId=parentId
		IF parentBread <> '' BEGIN
			SET parentBread = parentBread + '>' + parentTitle
			SET parentHier = parentHier + '>' + CONVERT(VARCHAR(25),parentId)
		END ELSE BEGIN
			SET parentBread = parentTitle
			SET parentHier = CONVERT(VARCHAR(25),parentId)
		END
		UPDATE Subjects SET hierarchy=parentHier, breadcrumb=parentBread WHERE subjectId=childId
		FETCH FROM cursor1 INTO
		childId, parentId
	END
	CLOSE cursor1
	DEALLOCATE cursor1
END;

$$;

-- File: Stored Procedures/Subjects/Subjects_GetList.sql
CREATE OR REPLACE PROCEDURE  public."Subjects_GetList"
(
    IN subjectIds TEXT,
    IN parentId INT DEFAULT -1
);
LANGUAGE plpgsql
AS $$
BEGIN
IF subjectIds <> '' BEGIN
	SELECT * INTO #subjects FROM public.SplitArray(subjectIds, ',')
	SELECT * FROM Subjects 
	WHERE subjectId IN (SELECT CONVERT(INT, value) FROM #subjects)
	AND parentId = CASE WHEN parentId >= 0 THEN parentId ELSE parentId END
	ORDER BY title ASC
END ELSE BEGIN
/* parentId only */
	SELECT * FROM Subjects 
	WHERE parentId = CASE WHEN parentId >= 0 THEN parentId ELSE parentId END
	ORDER BY title ASC
END
END;

$$;

-- File: Stored Procedures/Whitelist/Whitelist_Domain_Add.sql
CREATE OR REPLACE PROCEDURE  public."Whitelist_Domain_Add"
(
    IN domain VARCHAR(64)
);
LANGUAGE plpgsql
AS $$
DECLARE
    domainId INT;
BEGIN TRY
	INSERT INTO Whitelist_Domains (domain) VALUES (domain)
	END TRY
	BEGIN CATCH
	END CATCH
END;

$$;

-- File: Stored Procedures/Whitelist/Whitelist_Domain_Check.sql
CREATE OR REPLACE PROCEDURE  public."Whitelist_Domain_Check"
(
    IN domain VARCHAR(64)
);
LANGUAGE plpgsql
AS $$
BEGIN
SELECT COUNT(*) FROM Whitelist_Domains WHERE domain=domain
END;

$$;

-- File: Stored Procedures/Whitelist/Whitelist_Domain_Remove.sql
CREATE OR REPLACE PROCEDURE  public."Whitelist_Domain_Remove"
(
    IN domain VARCHAR(64)
);
LANGUAGE plpgsql
AS $$
BEGIN
DELETE FROM Whitelist_Domains WHERE domain=domain
END;

$$;

-- File: Stored Procedures/Whitelist/Whitelist_Domains_GetList.sql
CREATE OR REPLACE PROCEDURE public."Whitelist_Domains_GetList"
LANGUAGE plpgsql
AS $$
BEGIN
SELECT domain FROM Whitelist_Domains ORDER BY domain ASC
END;

$$;

-- File: Stored Procedures/Words/CommonWords_Add.sql
CREATE OR REPLACE PROCEDURE  public."CommonWords_Add"
(
    IN words TEXT
);
LANGUAGE plpgsql
AS $$
BEGIN
DELETE FROM CommonWords WHERE word IN (SELECT "value" FROM public.SplitArray(words, ','))
INSERT INTO CommonWords SELECT "value" AS word FROM public.SplitArray(words, ',')
END;

$$;

-- File: Stored Procedures/Words/CommonWords_GetList .sql
CREATE OR REPLACE PROCEDURE public."CommonWords_GetList"
LANGUAGE plpgsql
AS $$
BEGIN
SELECT * FROM CommonWords
END;

$$;

-- File: Stored Procedures/Words/Word_Add.sql
CREATE OR REPLACE PROCEDURE  public."Word_Add"
(
    IN word VARCHAR(64),
    IN subjectId INT DEFAULT 0,
    IN grammartype INT DEFAULT 0,
    IN score INT DEFAULT 1
);
LANGUAGE plpgsql
AS $$
DECLARE
    wordId INT;
BEGIN
IF(SELECT COUNT(*) FROM Words WHERE word=word AND grammartype=grammartype) = 0 BEGIN
		/* word doesn't exists */
		SET wordId = nextval('public."SequenceWords"')
		INSERT INTO Words (wordId, word, grammartype, score) 
		VALUES (wordId, word, grammartype, score)
	END ELSE BEGIN
		SELECT wordId = wordId FROM Words WHERE word=word
	END
	IF wordId IS NOT NULL BEGIN
		INSERT INTO SubjectWords (wordId, subjectId) VALUES (wordId, subjectId)
	END
	SELECT wordId
END;

$$;

-- File: Stored Procedures/Words/Words_BulkAdd.sql
CREATE OR REPLACE PROCEDURE  public."Words_BulkAdd"
(
    IN words TEXT,
    IN subjectId INT DEFAULT 0,
    IN grammartype INT DEFAULT 0,
    IN score INT DEFAULT 1
);
LANGUAGE plpgsql
AS $$
DECLARE
    word VARCHAR(32), wordId INT, cursor CURSOR;
BEGIN
SELECT "value" as word INTO #words FROM public.SplitArray(words, ',')
	SET cursor = CURSOR FOR
	SELECT word FROM #words
	OPEN cursor
	FETCH NEXT FROM cursor INTO word
	WHILE @@FETCH_STATUS = 0 BEGIN
		IF NOT EXISTS(SELECT * FROM Words WHERE word=word) BEGIN
			/* word doesn't exists */
			SET wordId = nextval('public."SequenceWords"')
			INSERT INTO Words (wordId, word, grammartype, score) 
			VALUES (wordId, word, grammartype, score)
		END ELSE BEGIN
			SELECT wordId = wordId FROM Words WHERE word=word
		END
		IF wordId IS NOT NULL AND wordId > 0 
		AND NOT EXISTS(SELECT * FROM SubjectWords WHERE subjectId=subjectId AND wordId=wordId) BEGIN
			INSERT INTO SubjectWords (wordId, subjectId) VALUES (wordId, subjectId)
		END
		FETCH NEXT FROM cursor INTO word
	END
	CLOSE cursor
	DEALLOCATE cursor
END;

$$;

-- File: Stored Procedures/Words/Words_GetList.sql
CREATE OR REPLACE PROCEDURE  public."Words_GetList"
(
    IN words TEXT
);
LANGUAGE plpgsql
AS $$
BEGIN
SELECT * INTO #words FROM public.SplitArray(words, ',')
SELECT w.* FROM Words w
WHERE word IN (SELECT value FROM #words)
END;

$$;
