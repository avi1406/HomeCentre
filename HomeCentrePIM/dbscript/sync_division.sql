CREATE OR REPLACE PROCEDURE public.sync_division(
	p_name text,
	p_id integer,
	p_isactive boolean)
LANGUAGE 'plpgsql'
AS $BODY$
BEGIN
    IF EXISTS (SELECT 1 FROM division WHERE  id =  p_id) THEN
        UPDATE division 
        SET name = p_name, isactive = p_isactive 
        WHERE id =  p_id;
    ELSE
        INSERT INTO division (name,code, id, isactive) 
        VALUES (p_name,p_id, p_id, p_isactive);
    END IF;
END;
$BODY$;

ALTER PROCEDURE public.sync_division(text, integer, boolean)
    OWNER TO lmdev_admin;