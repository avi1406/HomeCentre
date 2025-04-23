CREATE OR REPLACE PROCEDURE public.sync_department(
	g_departmentcode integer,
	g_groupcode integer,
	g_name text,
	g_isactive boolean)
LANGUAGE 'plpgsql'
AS $BODY$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM productgroup WHERE groupcode = g_groupcode) THEN
        RAISE EXCEPTION 'Group code % does not exist in the group table', g_groupcode;
    END IF;

    IF EXISTS (SELECT 1 FROM productdepartment WHERE groupcode = g_groupcode AND departmentcode = g_departmentcode) THEN
        UPDATE productdepartment SET name = g_name, isactive = g_isactive, groupcode = g_groupcode
		WHERE departmentcode = g_departmentcode;
    ELSE
        INSERT INTO productdepartment (departmentcode, code, groupcode, name, isactive) 
		VALUES (g_departmentcode, g_departmentcode, g_groupcode, g_name, g_isactive);
    END IF;
END;
$BODY$;

ALTER PROCEDURE public.sync_department(integer, integer, text, boolean)
    OWNER TO lmdev_admin;