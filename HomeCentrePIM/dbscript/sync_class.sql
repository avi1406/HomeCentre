CREATE OR REPLACE PROCEDURE public.sync_class(
	g_departmentcode integer,
	g_classcode integer,
	g_name text,
	g_isactive boolean)
LANGUAGE 'plpgsql'
AS $BODY$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM productdepartment WHERE departmentcode = g_departmentcode) THEN
        RAISE EXCEPTION 'department code % does not exist in the department table', g_departmentcode;
    END IF;

    IF EXISTS (SELECT 1 FROM productclass WHERE department = g_departmentcode AND classcode = g_classcode) THEN
        UPDATE productclass SET name = g_name, isactive = g_isactive, department = g_departmentcode
		WHERE classcode = g_classcode;
    ELSE
        INSERT INTO productclass (classcode, code, department, name, isactive) 
		VALUES (g_classcode, g_classcode, g_departmentcode, g_name, g_isactive);
    END IF;
END;
$BODY$;

ALTER PROCEDURE public.sync_class(integer, integer, text, boolean)
    OWNER TO lmdev_admin;