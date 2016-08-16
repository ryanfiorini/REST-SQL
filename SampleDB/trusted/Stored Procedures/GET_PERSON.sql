﻿CREATE PROCEDURE [trusted].[GET_PERSON]
(
	/*Returns Json*/
	/*Single Result*/
	@PERSON_ID INT,

	@MESSAGE_RESULT VARCHAR(256) OUTPUT,
	@SQL_ERROR_ID INT OUTPUT
)
AS
BEGIN

	SET NOCOUNT ON;

	IF @PERSON_ID IS NULL THROW 50001, '@PERSON_ID', 1;

	BEGIN TRY

		SELECT
			PERSON_ID personId,
			FIRST_NAME firstName,
			LAST_NAME lastName,
			EMAIL email
		FROM
			dbo.PERSON
		WHERE
			PERSON_ID = @PERSON_ID
		FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;

		RETURN 200;

	END TRY
	BEGIN CATCH

		--DO SOME LOGGING AND GET ERROR ID
		SET @SQL_ERROR_ID = 1234;
		SET @MESSAGE_RESULT = (SELECT 'ERROR OCCURED AND HAS BEEN LOGGED');

		RETURN 500;

	END CATCH

END;
