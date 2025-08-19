/* Set up the database */
IF NOT EXISTS(SELECT * FROM AppRoles) BEGIN
	INSERT INTO AppRoles ([Name])
	VALUES
	('admin'),
	('owner'),
	('manager'),
	('user')
END