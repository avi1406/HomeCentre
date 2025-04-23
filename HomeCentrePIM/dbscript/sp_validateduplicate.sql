CREATE OR REPLACE PROCEDURE public.sp_validateduplicate(
	deptcode character varying,
	classcode character varying,
	groupcode character varying,
	subclasscode character varying,
	productdesc character varying,
	planogramfamilycode character varying,
	INOUT _val integer DEFAULT 0)
LANGUAGE 'plpgsql'
AS $BODY$
begin
 
select count(*) from vw_columndata where dept_code=deptcode and class_code=classcode
and group_code=groupcode and subclass_code =subclasscode and product_desc=productdesc and planogram_family_code=planogramfamilycode into _val;
return;
end; 
$BODY$;