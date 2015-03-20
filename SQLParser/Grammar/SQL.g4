grammar SQL;

options
{
    language=CSharp;
}

//Everything inside the @header section will be placed at the start of your Parser class
@header {
	using System;
}
 
@parser::members
{
    protected const int EOF = Eof;
}
 
@lexer::members
{
    protected const int EOF = Eof;
    protected const int HIDDEN = Hidden;
}


/*
 * Parser Rules
 */

//parse : ( sql_stmt_list | error )* EOF;
parse : (sql_stmt_list | error);

error : UNEXPECTED_CHAR 
   { 
     throw new Exception("UNEXPECTED_CHAR=" + $UNEXPECTED_CHAR.text); 
   }
 ;

 
sql_stmt_list : ';'* sql_stmt ( ';'+ sql_stmt )* ';'* ;

sql_stmt : ( select_stmt) ;

 
select_stmt : select_or_values ( compound_operator select_or_values )*  ( order_by )?
 ;

order_by
	: K_ORDER K_BY op=(K_SUMRANK|K_BESTRANK) '(' + ')'			#orderBySpecial
	| K_ORDER K_BY ordering_term ( ',' ordering_term )*	 		#orderByDefault
;

select_or_values
 : K_SELECT  ( K_DISTINCT | K_ALL )? (top_keyword)? select_List_Item ( ',' select_List_Item  )*
   ( K_FROM ( table_List_Item ( ',' table_List_Item )* | join_clause ) )?
   ( K_WHERE expr )?
   ( K_SKYLINE K_OF exprSkyline (K_SAMPLE K_BY exprSkylineSample)? )?
 ;

type_name : name+ ( '(' signed_number ')' | '(' signed_number ',' signed_number ')' )?
 ;


/*
    SQL understands the following binary operators, in order from highest to
    lowest precedence:

    ||
    *    /    %
    +    -
    <<   >>   &    |
    <    <=   >    >=
    =    ==   !=   <>   IS   IS NOT   IN   LIKE   MATCH   REGEXP
    AND
    OR
*/




expr
 : literal_value																						#expropLiteral
 | column_term																							#expropDatabaseName
 | unary_operator expr																					#expropUnary
 | expr '||' expr																						#expropOr
 | expr ( '*' | '/' | '%' ) expr																		#expropPoint
 | expr ( '+' | '-' ) expr																				#expropLine
 | expr ( '<<' | '>>' | '&' | '|' ) expr																#expropDoubleOrder
 | expr ( '<' | '<=' | '>' | '>=' ) expr																#expropOrder
 | expr ( '=' | '==' | '!=' | '<>' | K_IS | K_IS K_NOT | K_IN | K_LIKE | K_MATCH ) expr					#expropequal
 | '{' exprOwnPreference '}'																			#exprexprOwnPreferenceOp																	
 | expr K_AND expr																						#exprexprand
 | expr K_OR expr																						#exprexpror
 | function_name '(' ( K_DISTINCT? expr ( ',' expr )* | '*' )? ')'										#exprfunction
 | '(' expr ')'																							#exprexprInBracket
 | K_CAST '(' expr K_AS type_name ')'																	#exprcast
 | expr K_NOT? ( K_LIKE | K_MATCH ) expr ( K_ESCAPE expr )?		#not
 | expr ( K_ISNULL | K_NOTNULL | K_NOT K_NULL )															#exprisNull
 | expr K_IS K_NOT? expr																				#exprisNot
 | expr K_NOT? K_BETWEEN expr K_AND expr																#exprnotBetween
 | expr K_NOT? K_IN ( '(' ( select_stmt												
                          | expr ( ',' expr )*
                          )? 
                      ')'															
                    | ( database_name '.' )? table_name )												#exprnotIn
 | ( ( K_NOT )? K_EXISTS )? '(' select_stmt ')'															#exprnotExists
 | K_CASE expr? ( K_WHEN expr K_THEN expr )+ ( K_ELSE expr )? K_END										#exprcase
 | column_term ('(' exprCategory ')')																	#orderbyCategory
 ;

 
exprSkyline
 : exprSkyline ',' exprSkyline																			#exprAnd   
 | column_term op=(K_LOW | K_HIGH | K_LOWDATE | K_HIGHDATE)	(signed_number (K_EQUAL | K_INCOMPARABLE))?	#preferenceLOWHIGH
 | column_term ('(' exprCategory ')')																	#preferenceCategory
 | column_term op=(K_AROUND | K_FAVOUR | K_DISFAVOUR) (signed_number|geocoordinate|column_term)			#preferenceAROUND
 | exprSkyline K_IS K_MORE K_IMPORTANT K_THAN exprSkyline												#preferenceMoreImportant

 ;

exprSkylineSample
 : exprSkylineSample ',' exprSkylineSample																#exprSampleAnd   
 | ('(' column_term ')')																				#preferenceSampleOne
 | ('(' column_term (',' column_term)* ')')																#preferenceSampleMultiple

 ;

 exprCategory
	: literal_value																						#opLiteral
	| '{' exprOwnPreference '}'																			#exprOwnPreferenceOp																	
	| exprCategory ( '>>'  | '==') exprCategory															#opDoubleOrder
	//OTHERS keywords are only possible with greater than
		//With the EQUAL keyword it would be too much coding		(i.e. blue >> red == OTHERS EQUAL --> blue == OTHERS EQUAL)
		//With the INCOMPARABLE keyword it would be a contradiction (i.e. red == OTHERS INCOMPARABLE)
	| exprCategory ( '>>') exprOthers																	#opExprOthersAfter
	| exprOthers ( '>>') exprCategory																	#opExprOthersBefore
	;


exprOthers
	: K_OTHERS (K_EQUAL)
	| K_OTHERS (K_INCOMPARABLE);

geocoordinate :'(' expr ',' expr ')';

//For own preferences 
exprOwnPreference
	: column_term
	| exprOwnPreference ',' exprOwnPreference
;

column_term : ( ( database_name '.' )? table_name '.' )? column_name;
ordering_term : expr ( K_ASC | K_DESC )? ;

select_List_Item  : '*' | table_name '.' '*'  | expr ( K_AS? column_alias )?;

table_List_Item
 : ( database_name '.' )? table_name ( K_AS? table_alias )?
 | '(' ( table_List_Item ( ',' table_List_Item )*
       | join_clause )
   ')' ( K_AS? table_alias )?
 | '(' select_stmt ')' ( K_AS? table_alias )?
 ;

join_clause : table_List_Item ( join_operator table_List_Item join_constraint )* ;

join_operator: ',' | ( K_LEFT K_OUTER? | K_INNER | K_CROSS )? K_JOIN;

join_constraint: ( K_ON expr  | K_USING '(' column_name ( ',' column_name )* ')' )?;

compound_operator: K_UNION | K_UNION K_ALL | K_INTERSECT | K_EXCEPT;

signed_number : ( '+' | '-' )? NUMERIC_LITERAL;

literal_value
 : NUMERIC_LITERAL
 | STRING_LITERAL
 | K_NULL
 | K_CURRENT_TIME
 | K_CURRENT_DATE
 | K_CURRENT_TIMESTAMP
 ;

unary_operator
 : '-'
 | '+'
 | '~'
 | K_NOT
 ;

error_message: STRING_LITERAL;


column_alias: IDENTIFIER | STRING_LITERAL;

keyword
 : K_ALL
 | K_AND
 | K_AS
 | K_ASC
 | K_BETWEEN
 | K_BY
 | K_CASE
 | K_CAST
 | K_CROSS
 | K_CURRENT_DATE
 | K_CURRENT_TIME
 | K_CURRENT_TIMESTAMP
 | K_DESC
 | K_DISTINCT
 | K_ELSE
 | K_END
 | K_ESCAPE
 | K_EXCEPT
 | K_EXISTS
 | K_FROM
 | K_FULL
 | K_IN
 | K_INNER
 | K_INTERSECT
 | K_IS
 | K_ISNULL
 | K_JOIN
 | K_LEFT
 | K_LIKE
 | K_MATCH
 | K_NO
 | K_NOT
 | K_NOTNULL
 | K_NULL
 | K_OF
 | K_ON
 | K_OR
 | K_ORDER
 | K_OUTER
 | K_RIGHT
 | K_SELECT
 | K_TOP
 | K_TABLE
 | K_THEN
 | K_UNION
 | K_USING
 | K_VALUES
 | K_WHEN
 | K_WHERE
 //Preference
 | K_AROUND
 | K_DISFAVOUR
 | K_FAVOUR
 | K_HIGH
 | K_LOW
 | K_HIGHDATE
 | K_LOWDATE
 | K_OTHERS
 | K_EQUAL
 | K_INCOMPARABLE
 | K_SKYLINE
 | K_SUMRANK
 | K_BESTRANK
 | K_MORE
 | K_IMPORTANT
 | K_THAN
 | K_SAMPLE
 ;


name : any_name;

function_name : any_name;

database_name : any_name;

table_name : any_name;

new_table_name : any_name;

column_name : any_name;

table_alias : any_name;

top_keyword :  K_TOP NUMERIC_LITERAL;

any_name : IDENTIFIER 
 | keyword
 | STRING_LITERAL
 | '(' any_name ')'
 ;

SCOL : ';';
DOT : '.';
OPEN_PAR : '(';
CLOSE_PAR : ')';
COMMA : ',';
ASSIGN : '=';
STAR : '*';
PLUS : '+';
MINUS : '-';
TILDE : '~';
PIPE2 : '||';
DIV : '/';
MOD : '%';
LT2 : '<<';
GT2 : '>>';
AMP : '&';
PIPE : '|';
LT : '<';
LT_EQ : '<=';
GT : '>';
GT_EQ : '>=';
EQ : '==';
NOT_EQ1 : '!=';
NOT_EQ2 : '<>';

// SQL Keywords
K_ALL : A L L;
K_AND : A N D;
K_AS : A S;
K_ASC : A S C;
K_BETWEEN : B E T W E E N;
K_BY : B Y;
K_CASE : C A S E;
K_CAST : C A S T;
K_CROSS : C R O S S;
K_CURRENT_DATE : C U R R E N T '_' D A T E;
K_CURRENT_TIME : C U R R E N T '_' T I M E;
K_CURRENT_TIMESTAMP : C U R R E N T '_' T I M E S T A M P;
K_DESC : D E S C;
K_DISTINCT : D I S T I N C T;
K_ELSE : E L S E;
K_END : E N D;
K_ESCAPE : E S C A P E;
K_EXCEPT : E X C E P T;
K_EXISTS : E X I S T S;
K_FROM : F R O M;
K_FULL : F U L L;
K_IN : I N;
K_INNER : I N N E R;
K_INTERSECT : I N T E R S E C T;
K_IS : I S;
K_ISNULL : I S N U L L;
K_JOIN : J O I N;
K_LEFT : L E F T;
K_LIKE : L I K E;
K_MATCH : M A T C H;
K_NO : N O;
K_NOT : N O T;
K_NOTNULL : N O T N U L L;
K_NULL : N U L L;
K_OF : O F;
K_ON : O N;
K_OR : O R;
K_ORDER : O R D E R;
K_OUTER : O U T E R;
K_RIGHT : R I G H T;
K_SELECT : S E L E C T;
K_TABLE : T A B L E;
K_THEN : T H E N;
K_TOP : T O P;
K_UNION : U N I O N;
K_USING : U S I N G;
K_VALUES : V A L U E S;
K_WHEN : W H E N;
K_WHERE : W H E R E;
//Preference Keywords
K_AROUND : A R O U N D;
K_DISFAVOUR : D I S F A V O U R;
K_FAVOUR : F A V O U R;
K_HIGH : H I G H;
K_LOW : L O W;
K_HIGHDATE: H I G H D A T E;
K_LOWDATE : L O W D A T E;
K_OTHERS : O T H E R S ;
K_EQUAL : E Q U A L;
K_INCOMPARABLE : I N C O M P A R A B L E;
K_SKYLINE : S K Y L I N E;
K_SUMRANK : S U M '_' R A N K;
K_BESTRANK : B E S T '_' R A N K;
K_MORE: M O R E;
K_IMPORTANT: I M P O R T A N T;
K_THAN: T H A N;
K_SAMPLE: S A M P L E;



 /*
 * Lexer Rules
 */

IDENTIFIER
 : '"' (~'"' | '""')* '"'
 | '`' (~'`' | '``')* '`'
 | '[' ~']'* ']'
 | [a-zA-Z_] [a-zA-Z_0-9]*
 ;

NUMERIC_LITERAL
 : DIGIT+ ( '.' DIGIT* )? ( E [-+]? DIGIT+ )?
 | '.' DIGIT+ ( E [-+]? DIGIT+ )?
 ;


STRING_LITERAL
 : '\'' ( ~'\'' | '\'\'' )* '\''
 ;

SINGLE_LINE_COMMENT
 : '--' ~[\r\n]* -> channel(HIDDEN)
 ;

MULTILINE_COMMENT
 : '/*' .*? ( '*/' | EOF ) -> channel(HIDDEN)
 ;

SPACES
 : [ \u000B\t\r\n] -> channel(HIDDEN)
 ;

UNEXPECTED_CHAR : . ;

 
fragment DIGIT : [0-9];

fragment A : [aA];
fragment B : [bB];
fragment C : [cC];
fragment D : [dD];
fragment E : [eE];
fragment F : [fF];
fragment G : [gG];
fragment H : [hH];
fragment I : [iI];
fragment J : [jJ];
fragment K : [kK];
fragment L : [lL];
fragment M : [mM];
fragment N : [nN];
fragment O : [oO];
fragment P : [pP];
fragment Q : [qQ];
fragment R : [rR];
fragment S : [sS];
fragment T : [tT];
fragment U : [uU];
fragment V : [vV];
fragment W : [wW];
fragment X : [xX];
fragment Y : [yY];
fragment Z : [zZ];
