CREATE OR REPLACE PROCEDURE public.updatetablemetadata(
	id integer,
	c_name character varying)
LANGUAGE 'plpgsql'
AS $BODY$
 BEGIN

INSERT INTO role (Name, description ) Values (c_Name,description );

END 
$BODY$;

ALTER PROCEDURE public.updatetablemetadata(integer, character varying)
    OWNER TO lmdev_admin;