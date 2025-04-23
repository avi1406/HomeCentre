CREATE OR REPLACE PROCEDURE public.role(
    )
LANGUAGE 'plpgsql'
AS $BODY$
BEGIN
IF EXISTS (SELECT name FROM role WHERE id = 6) THEN
  UPDATE role SET name = 'BASIC ROLE', description ='Basic' WHERE id = 6;
ELSE
  INSERT INTO role ( name,description) VALUES ('BASIC ROLE', 'Basic');
END IF;
END;
$BODY$;

 

ALTER PROCEDURE public.role()
    OWNER TO lmdev_admin;