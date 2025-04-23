CREATE OR REPLACE PROCEDURE public.sync_brand(
	p_name text,
	p_brandcode integer,
	p_isactive boolean)
LANGUAGE 'plpgsql'
AS $BODY$
BEGIN
    IF EXISTS (SELECT 1 FROM brand WHERE brandcode = p_brandcode) THEN
        UPDATE brand 
        SET name = p_name, isactive = p_isactive 
        WHERE brandcode = p_brandcode;
    ELSE
        INSERT INTO brand (name, brandcode, isactive) 
        VALUES (p_name, p_brandcode, p_isactive);
    END IF;
END;
$BODY$;

ALTER PROCEDURE public.sync_brand(text, integer, boolean)
    OWNER TO lmdev_admin;