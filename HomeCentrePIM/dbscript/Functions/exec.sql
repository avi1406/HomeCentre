CREATE OR REPLACE FUNCTION public."exec"(
	text)
    RETURNS SETOF record 
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
    ROWS 1000

AS $BODY$
BEGIN 
    RETURN QUERY EXECUTE $1 ; 
END 
$BODY$;

ALTER FUNCTION public."exec"(text)
    OWNER TO lmdev_admin;