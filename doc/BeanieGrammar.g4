parser grammar BeanieGrammar;

options { 
    tokenVocab=BeanieLexer;
}

program
    : namespace_declaration
    | declaration*;

// Declarations
declaration
    : namespace_declaration
    | class_declaration
    | enum_declaration
    | union_declaration
    | interface_declaration
    | type_declaration;

access_modifier
    : PUBLIC
    | PRIVATE
    | PROTECTED;

// Classes
class_declaration
    : attribute* access_modifier CLASS (generic_name | IDENTIFIER) inheritance? CURLY_LEFT class_body CURLY_RIGHT;

class_body
    : (field_declaration | property_declaration | method_definition | method_declaration | constructor_declaration)*;

field_declaration
    : attribute* access_modifier type_expression IDENTIFIER (EQUALS expression)? SEMICOLON;

property_declaration
    : attribute* type_expression IDENTIFIER CURLY_LEFT property_getter_setter (COMMA property_getter_setter)? COMMA? CURLY_RIGHT;

property_getter_setter
    : attribute* access_modifier (GET | SET) (EQUALS expression)?;

constructor_declaration
    : attribute* access_modifier IDENTIFIER PAREN_LEFT constructor_param_list? PAREN_RIGHT CURLY_LEFT function_body CURLY_RIGHT;

constructor_param_list
    : constructor_param (COMMA constructor_param)* COMMA?;

constructor_param
    : attribute* (THIS | type_expression) IDENTIFIER;

method_definition
    : attribute* access_modifier type_expression IDENTIFIER PAREN_LEFT param_list? PAREN_RIGHT CURLY_LEFT function_body CURLY_RIGHT;

method_declaration
    : attribute* access_modifier type_expression IDENTIFIER PAREN_LEFT param_list? PAREN_RIGHT SEMICOLON;

inheritance
    : COLON type_expr_list?;

// Enums
enum_declaration
    : attribute* access_modifier ENUM (generic_name | IDENTIFIER) PAREN_LEFT param_list? PAREN_RIGHT inheritance? CURLY_LEFT enum_body CURLY_RIGHT;

enum_body
    : (enum_case | method_definition)*;

enum_case
    : IDENTIFIER (PAREN_LEFT expr_list? PAREN_RIGHT)? SEMICOLON;

// Unions
union_declaration
    : attribute* access_modifier UNION (generic_name | IDENTIFIER) inheritance? CURLY_LEFT union_body CURLY_RIGHT;

union_body
    : (union_case | method_definition)*;

union_case
    : IDENTIFIER PAREN_LEFT param_list? PAREN_RIGHT SEMICOLON;

// Interfaces
interface_declaration
    : attribute* access_modifier INTERFACE (generic_name | IDENTIFIER) inheritance? CURLY_LEFT interface_body CURLY_RIGHT;

interface_body
    : (method_declaration | method_definition | property_declaration)*;

// Types
type_declaration
    : attribute* TYPE (generic_name | IDENTIFIER) EQUALS type_expression SEMICOLON;

type_expression
    : access_expr (generic | compile_generic)?;

type_expr_list
    : type_expression (COMMA type_expression)* COMMA?;
    
// Generics
generic_name
    : IDENTIFIER (LESS_THAN (IDENTIFIER (COMMA IDENTIFIER)*)? GREATER_THAN
                  | LESS_THAN SQUARE_LEFT param_list SQUARE_RIGHT GREATER_THAN);

// Functions
function_body
    : statement*;

param_list
    : parameter (COMMA parameter)* COMMA?;

parameter
    : type_expression IDENTIFIER;

statement
    : variable_declaration
    | expression_statement
    | return_statement;

variable_declaration
    : type_expression IDENTIFIER (EQUALS expression)? SEMICOLON;

expression_statement
    : expression SEMICOLON;

return_statement
    : RETURN expression SEMICOLON;

// Expressions
expr_list
    : expression (COMMA expression)* COMMA?;

expression
    : logical_or;

logical_or
    : logical_and (OR logical_and)*;

logical_and
    : equality_expr (AND equality_expr)*;

equality_expr
    : relational_expr ((EQUALITY | NOT_EQUAL) relational_expr)*;

relational_expr
    : additive_expr ((GREATER_THAN | LESS_THAN | GREATER_THAN_EQUALITY | LESS_THAN_EQUALITY) additive_expr)*;

additive_expr
    : multiplicative_expr ((PLUS | MINUS) multiplicative_expr)*;

multiplicative_expr
    : unary_expr ((STAR | SLASH | PERCENT) unary_expr)*;

unary_expr
    : (BANG | MINUS | PLUS)* primary_expr;

primary_expr
    : LITERAL_NUMBER
    | LITERAL_STRING
    | LITERAL_BOOL
    | THIS
    | CODE_BLOCK
    | PAREN_LEFT expression PAREN_RIGHT 
    | block_expr
    | if_expr
    | match_expr
    | function_call_expr
    | macro_call_expr
    | access_expr
    | lambda_expr
    | type_expression;
    
block_expr
    : CURLY_LEFT statement* CURLY_RIGHT;

if_expr
    : IF expression CURLY_LEFT statement* CURLY_RIGHT (IF ELSE expression CURLY_LEFT statement* CURLY_RIGHT)* (ELSE CURLY_LEFT statement* CURLY_RIGHT)?;

match_expr
    : MATCH IDENTIFIER CURLY_LEFT match_case_list CURLY_RIGHT;

match_case_list
    : match_case (COMMA match_case)* COMMA?;

match_case
    : PAREN_LEFT match_params? PAREN_RIGHT ARROW block_expr;

match_params
    : match_param (COMMA match_param)* COMMA?;

match_param
    : UNDERSCORE
    | expression;

function_call_expr
    : (generic_name | IDENTIFIER) PAREN_LEFT expr_list? PAREN_RIGHT;

macro_call_expr
    : AT IDENTIFIER PAREN_LEFT expr_list? PAREN_RIGHT;

access_expr
    : expression DOT expression;
    
lambda_expr
    : PAREN_LEFT ident_list PAREN_RIGHT ARROW block_expr;

// Identifiers
namespace_declaration
    : NAMESPACE qualified_name SEMICOLON declaration*;

qualified_name
    : (generic_name | IDENTIFIER) (DOT qualified_name)*;
    
ident_list
    : IDENTIFIER (COMMA IDENTIFIER)* COMMA?;

// Attribute
attribute
    : SQUARE_LEFT attribute_list SQUARE_RIGHT;

attribute_list
    : attribute_body (COMMA attribute_body)? COMMA?;

attribute_body
    : AT? qualified_name (PAREN_LEFT param_list? PAREN_RIGHT)?;