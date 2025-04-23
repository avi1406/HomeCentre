CREATE OR REPLACE FUNCTION public.get_hierarchy(
	p_code integer,
	p_isclass boolean)
    RETURNS TABLE(businesscode bigint, businessname text, divisioncode bigint, divname text, groupcode bigint, groupname text, deptcode bigint, deptname text, classcode bigint, classname text, subclasscode bigint, subname text) 
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
    ROWS 1000

AS $BODY$
BEGIN
   if  p_isclass then 
    RETURN QUERY SELECT business, business_name, division, div_name, group_no, group_name, 
	dept, dept_name, class, class_name, subclass, sub_name 
	FROM public.merchandise_hierarchy
    WHERE
        class=p_code ;
	else
	RETURN QUERY SELECT business, business_name, division, div_name, group_no, group_name, 
	dept, dept_name, class, class_name, subclass, sub_name
	FROM public.merchandise_hierarchy
    WHERE
        subclass=p_code ;
	END IF;	
END;
$BODY$;

ALTER FUNCTION public.get_hierarchy(integer, boolean)
    OWNER TO lmdev_admin;
