CREATE OR REPLACE FUNCTION public.gettablemetadatabyid(
	_id integer,
	_name character varying)
    RETURNS TABLE(table_id bigint) 
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
    ROWS 1000

AS $BODY$
BEGIN
   RETURN QUERY
    SELECT id, name FROM tablemetadata WHERE id = _id;
END;
$BODY$;

ALTER FUNCTION public.gettablemetadatabyid(integer, character varying)
    OWNER TO lmdev_admin;
