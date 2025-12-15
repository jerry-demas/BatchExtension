USE [Logging]
GO

-- Object:  Table [dbo].[BatchExtension]    Script Date: 11/3/2025 9:05:37 AM --
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.BatchExtension') AND type = 'U')
BEGIN
CREATE TABLE [dbo].[BatchExtension](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Logged] [datetime2](3) NOT NULL,
	[Level] [nvarchar](50) NOT NULL,
	[Message] [nvarchar](max) NULL,
	[Logger] [nvarchar](256) NULL,
	[Exception] [nvarchar](max) NULL,
	[UserName] [nvarchar](256) NULL,
 CONSTRAINT [PK_BatchExtension] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
END


