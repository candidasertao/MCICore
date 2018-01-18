CREATE FUNCTION [dbo].[CommaSeparatedListToSingleColumn]
( 
    @cslist VARCHAR(MAX)
) 
RETURNS @t TABLE 
( 
    Item VARCHAR(64) 
) 
BEGIN  
    DECLARE @spot SMALLINT, @str VARCHAR(8000), @sql VARCHAR(8000)  
     
    WHILE @cslist <> ''  
    BEGIN  
        SET @spot = CHARINDEX(',', @cslist)  
        IF @spot>0  
            BEGIN  
                SET @str = LEFT(@cslist, @spot-1)  
                SET @cslist = RIGHT(@cslist, LEN(@cslist)-@spot)  
            END  
        ELSE  
            BEGIN  
                SET @str = @cslist  
                SET @cslist = ''  
            END  
        INSERT @t SELECT @str 
    END  
    RETURN 
END