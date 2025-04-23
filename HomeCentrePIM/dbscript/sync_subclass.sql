CREATE OR REPLACE PROCEDURE public.sync_subclass(
	g_departmentcode integer,
	g_classcode integer,
	g_subclasscode integer,
	g_name text,
	g_isactive boolean)
LANGUAGE 'plpgsql'  
AS $BODY$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM productdepartment WHERE departmentcode = g_departmentcode) THEN
        RAISE EXCEPTION 'department code % does not exist in the department table', g_departmentcode;
    END IF;
	
	IF NOT EXISTS (SELECT 1 FROM productclass WHERE classcode = g_classcode) THEN
        RAISE EXCEPTION 'class code % does not exist in the class table', g_classcode;
    END IF;

    IF EXISTS (SELECT 1 FROM productsubclass WHERE department = g_departmentcode AND classid = g_classcode and subclasscode = g_subclasscode) THEN
        UPDATE productsubclass SET name = g_name, isactive = g_isactive, department = g_departmentcode, classid = g_classcode
		WHERE subclasscode = g_subclasscode;
    ELSE
        INSERT INTO productsubclass (subclasscode, code, classid, department, name, isactive) 
		VALUES (g_subclasscode, g_subclasscode, g_classcode, g_departmentcode, g_name, g_isactive);
    END IF;
END;
$BODY$;

ALTER PROCEDURE public.sync_subclass(integer, integer, integer, text, boolean)
    OWNER TO lmdev_admin;