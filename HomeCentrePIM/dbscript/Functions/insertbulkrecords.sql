-- FUNCTION: public.fn_insertbulkrecords(character varying)

-- DROP FUNCTION IF EXISTS public.fn_insertbulkrecords(character varying);

CREATE OR REPLACE FUNCTION public.fn_insertbulkrecords(
	json_data character varying)
    RETURNS TABLE(dept_code text, class_code text, subclass_code text, planogram_family_code text, group_code text, product_desc text) 
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
    ROWS 1000

AS $BODY$
BEGIN
   DROP TABLE IF EXISTS TempTable; 
    CREATE   TABLE   TempTable 
        (dept_code text,
        class_code text,
        subclass_code text,
        planogram_family_code text,
        group_code text,
        product_desc text)
     ;

    INSERT INTO TempTable (dept_code, class_code, subclass_code, planogram_family_code, group_code, product_desc)
	
 WITH data (json_value) AS (
        VALUES (json_data::jsonb)
    )
    SELECT
        e->>'Dept Code' as dept_code ,
        e->>'Class Code' as class_code,
        e->>'Subclass Code' as subclass_code,
        e->>'Planogram/Family Code' as planogram_family_code,
        e->>'Group Code' as group_code,
        e->>'Product Desc' as product_desc
    FROM data d
    CROSS JOIN jsonb_array_elements(d.json_value) AS e;

     	Return query SELECT TempTable.dept_code,
				 TempTable.class_code ,
        TempTable.subclass_code ,
        TempTable.planogram_family_code ,
        TempTable.group_code ,
        TempTable.product_desc 
				 
				 FROM TempTable TempTable				 
				 INNER JOIN vw_columndata ON TempTable.dept_code = vw_columndata.dept_code and
	TempTable.class_code = vw_columndata.class_code and TempTable.subclass_code = vw_columndata.subclass_code and 
	TempTable.planogram_family_code = vw_columndata.planogram_family_code and TempTable.group_code = vw_columndata.group_code and 
				 TempTable.product_desc = vw_columndata.product_desc;
				 

END 
$BODY$;

ALTER FUNCTION public.fn_insertbulkrecords(character varying)
    OWNER TO pim_prod_admin;
