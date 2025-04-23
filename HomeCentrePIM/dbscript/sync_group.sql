CREATE OR REPLACE PROCEDURE public.sync_group(
	g_divisioncode integer,
	g_groupcode integer,
	g_name text,
	g_isactive boolean)
LANGUAGE 'plpgsql'
AS $BODY$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM division WHERE id = g_divisioncode) THEN
        RAISE EXCEPTION 'Division code % does not exist in the division table', g_divisioncode;
    END IF;

    IF EXISTS (SELECT 1 FROM productgroup WHERE groupcode = g_groupcode AND division = g_divisioncode) THEN
        UPDATE productgroup SET name = g_name, isactive = g_isactive, division = g_divisioncode
		WHERE groupcode = g_groupcode;
    ELSE
        INSERT INTO productgroup (division, groupcode, name, isactive) 
		VALUES (g_divisioncode, g_groupcode, g_name, g_isactive);
    END IF;
END;
$BODY$;

ALTER PROCEDURE public.sync_group(integer, integer, text, boolean)
    OWNER TO lmdev_admin;