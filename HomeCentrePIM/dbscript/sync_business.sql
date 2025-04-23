CREATE OR REPLACE PROCEDURE public.sync_business(
	p_name text,
	p_businesscode integer,
	p_isactive boolean)
LANGUAGE 'plpgsql'
AS $BODY$
BEGIN
    IF EXISTS (SELECT 1 FROM business WHERE businesscode = p_businesscode) THEN
        UPDATE business 
        SET name = p_name, isactive = p_isactive 
        WHERE businesscode = p_businesscode;
    ELSE
        INSERT INTO business (name, businesscode, isactive) 
        VALUES (p_name, p_businesscode, p_isactive);
    END IF;
END;
$BODY$;

ALTER PROCEDURE public.sync_business(text, integer, boolean)
    OWNER TO lmdev_admin;