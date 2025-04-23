CREATE OR REPLACE FUNCTION public.attributevalue(
	lcolumn text,
	ltable text)
    RETURNS text
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
declare
    returnval text;
BEGIN 
    select string_agg(Code::text, ',') into  returnval from exec(
	  FORMAT('SELECT distinct CAST(%s as text) from %s',lcolumn,ltable)) 
	  as t(Code text) ; 
	  return returnval;
END ;
$BODY$;

ALTER FUNCTION public.attributevalue(text, text)
    OWNER TO lmdev_admin;
