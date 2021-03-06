﻿CREATE TABLE [dbo].[REST_SQL_VALIDATORS]
(
	[SPECIFIC_SCHEMA] NVARCHAR(128) NOT NULL , 
    [SPECIFIC_NAME] NVARCHAR(128) NOT NULL, 
    [PARAMETER_NAME] NVARCHAR(128) NOT NULL,
	[NUMERIC_MINIMUM_VALUE] BIGINT  NULL, 
    [NUMERIC_MAXIMUM_VALUE] BIGINT  NULL, 
    [REGULAR_EXPRESSION] NVARCHAR(2056) NULL, 
    CONSTRAINT [PK_PARAMETER_VALIDATORS] PRIMARY KEY ([SPECIFIC_SCHEMA], [SPECIFIC_NAME], [PARAMETER_NAME]) 
)
