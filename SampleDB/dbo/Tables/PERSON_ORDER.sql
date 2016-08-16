﻿CREATE TABLE [dbo].[PERSON_ORDER]
(
	PERSON_ORDER_ID INT IDENTITY(1,1) NOT NULL,
	PERSON_ID INT NOT NULL,
	ORDER_NAME VARCHAR(16) NOT NULL,
	ACTIVE_START DATETIME2(0) NOT NULL
	CONSTRAINT PK_PERSON_ORDER PRIMARY KEY (PERSON_ORDER_ID)
)
