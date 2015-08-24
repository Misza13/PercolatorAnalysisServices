# PercolatorAnalysisService
A Linq to MDX ORM

X - Intro to Percolator Analysis Servcies (PAS)
	A - Background
	B - Overview

1 - Model
	A - Generator
	B - Using Generated Cube Obects

2 - Objects
	A - Sets, Members and Other PAS Objects
	B - SubCubes
	C - Object Methods

3 - Queries
	A - Using Standard Linq Query Syntax
	B - Using PAS Explicit Syntax
	C - Using the Good Ol' String Query

4 - Mapping
	A - Using User Created Objects
	B - Using Anonymous Objects
	C - Using the Percolate Method with PAS Explicit Syntax

5 - Examples
	A - MDX Functions as PAS Methods
	B - Creating Query Scoped Calculated Members and Sets
	C - Querying Using LINQ Standard Syntax
	D - Querying Using Percolator Explicit Methods

6 - Things to Know


////////////////////////////////////////////////////////////////////////////////////////////////
X - INTRO TO PERCOLATOR ANALYSIS SERVICES (PAS)

---- X(A) - BACKGROUND - ----

Percolator Analysis Services (PAS) is a lightweight ORM that was built for our small company for interal use, but after seeing the lack 
of options for querying Analysis Services using .NET obects, we decided to throw it out there if anyone else wants to use it.  
As this was built for our company specifically, I only implemented commonly used MDX functions that we typically use.  
If enough people ask for other methods, then we will implement those as well.

---- X(B) - OVERVIEW - ----
	
	PAS consists of three main parts: 
	1) The generation template
	2) The Percolator objects and methods
	3) The Query Functionality
	
The generation template queries the Analysis Server for its schema info, and generates the appropriate objects,
mimicing the cube.  The Percolator obects and methods are used to construct different members or sets to be used in the query, 
and the different method names that are present in the MDX syntax (well, a number of them anyway). Once any and all members and sets
are created, you can use the standard LINQ query syntax that we're used to in SQL Database ORMs, a more explicit syntax built into the library,
or simply pass in a string query to the execute method.  

** NOTE -> Refer to the "Things to Know" section at the end for a list of notes pertaining to using this library.

/////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////
1 - MODEL																					   //
/////////////////////////////////////////////////////////////////////////////////////////////////
---- 1(A) - GENERATOR - ----

Open up the "PercolatorCube.tt" file inside the "PAS_GENERATOR" folder that was included in your project. The ConnectionString property is what the 
generator template uses to connect to the Analsis Services server, and retrieve the schema information to generate the appropriate classes.  
If you have a connection string you wish to be used in the app or web config for you application, 
enter in the name of the connection string in the ConfigFileName property.  If no name is entered, the provider will default to the
connection string you entered in for the ConnectionStringProperty. 

The DBName is the name you want the main database class to be.  If no name is provided, it will default to the database name found 
durring the schema query.  The Namespace property is the namespace that will be used for the classes. This property must be filled in.

There are also four booleans to set whether to include hidden objects or not. 

To generate your classes, input the needed information in the "PecolatorCube.tt" file, and click "Save".  A generated cs file will appear
below the "PercolatorCube.tt" file containing your generated cube objects.  Any manual changes to this cs file will be erased durring
any generations, so if you wish to change anything, change the template files themselves.  The Inlcude file contains the classes and methods 
to query the database and prepare a collection of objects, and the Generator file is what outputs the classes to the cs file.

---- 1(B) - USING GENERATED CUBE OBJECTS - ----

The generator outputs a class for each dimension found on a cube, and in each dimension class are the properties for each attribute in 
that dimension.  Once all the dimensions are done, a class is generated for the cube itself.  This cube class contains a property for 
each of the previous dimension classes, as well as properties for each measure belonging to that cube.  Also, inside each cube class
is a static property that returns the cube to give access to the cube's members without having to open a new database context. 
Finally, the main database class is made (the one you chose the name for in the tt properties) which contains the neccessary provider 
information to query the cube. Remember, this final class has properties and methods that open connections which need to be disposed of.  
Simply using a "using" block handles everything.

///////////////////////////////////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////////////////////////////
2 - OBJECTS																						 //
///////////////////////////////////////////////////////////////////////////////////////////////////

---- 2(A) - SETS, MEMBERS AND OTHER PAS OBJECTS - ----
The two main objects for PAS are the "Set" and "Member" objects.  They are used to create and store MDX sets or members
that are created using the MDX methods or created by the user (i.e. query scoped created members and sets).  The base class for these two objects 
is ICubeObect, which every Percolator Analysis Services class inherits from, similar to how all classes inherit from object.

To create a simple member or set => (as of version 0.2)
	
	The "Member" and "Set" classes each have a static "Create<>" method now, and the generic type is the cube type you wish to 
	use the objects of.

	var myAwesomeMember = Member.Create<SalesCube>(x => x.WholesaleDollars & x.Calendar.Year["2015"]);
	var myAwesomeSet = Set.Create<SalesCube>(x => x.Items.ItemID.Children * x.Stores.StoreID.children);

	**Notice the two overloaded operators "*" and "&". The "*" operator returns a cross joined set, and the "&" operator returns
		a comma seperated tuple. (I tried overloading the "," and ";" but to no avail.  The "&" seemed the next logical choice) 

	Also, in an attempt to always have a "Plan B" for situations where the translator isn't quite giving you what you want,
	Sets and Members are implicitly convertable to strings.

	Set mySet = "{[Calendar].[Year].Children * [Stores].[Store ID].Children}";

	Now, this example is very easily done using the PAS objects, but this example is just to illustrate that you can use strings
	to create Members and Sets if the built in PAS functionality isn't able to give you exaclty what you are looking for.
	"mySet" can then be inlcuded in the query, and that set will be translated exactly how the string is that you originally gave it.
	

The "Attribute" and "Level" classes represent an attribute or hierarchy level from a dimension in the cube.  Each attribute has a number 
of different properties for functions such as "Children" or "AllMembers".  If a function property is not available, you can use the 
".Function(string function)" method in the Attribute class, and pass is a string of the function. Also, to access a member from the attibute or level, 
use the C# index syntax and pass in the string of the member name.  The square brackets around the member name are automatically placed in for you, 
and if you begin the member name with "&", then the "&" will be placed before the square brackes to acces the member by its address.

Attribute and Level usage examples in LINQ query =>
	db.Stores.StoreName.Children //Amounts to "[Stores].[Store Name].Children" in the query
	db.Stores.StoreName.This // Amounts to "[Stores].[Store Name].[Store Name]"
	db.Stores.StoreID["&10423"] //Amounts to "[Stores].[Store ID].&[10423]"
	db.Calendar.CalendarHierarchy.Year["2015", "Quarter1", "January"] //Amounts to "[Calendar].[Calendar Hierarchy].[Year].[2015].[Quarter 1].[January]"
	db.Calendar.CalendarHierarchy.Year.Function("Count") // Amounts to "[Calendar].[Calendar Hierarchy].[Year].Count"

It is also possible to create cube wide query scoped calulated members and sets in your code.  Each cube class generated in the PAS generator is a partial
class.  Simply create another partial class to your cube class and add in AS PROPERTIES the calculated member or set.

	public Member DollarsFor2014 { get { return new Member(this.WholesaleDollars, this.Calendar.Year["2014"]); } }

The "DollarsFor2014" reference will now show up in you cube object when you query against it.

Methods can also be used =>
	//This method returns the most current years back from the int provided, orded by the year descending where the year contains at least one transaction
	public Set MostCurrentYears(int numberOfYears)
	{
		return Mdx.Head(
			Mdx.Order(
				Mdx.Filter(
					this.Calendar.Year.Children, 
					() => this.TransactionCount > 0), 
				this.Calendar.Year, OrderType.BDESC), 
			numberOfYears)
	}

	Since this method returns a Set, it can be included directly in a LINQ query.
	using(var db = new MyCubeDatabase())
	{
		var q = from cube in db.SalesCube
				select new MdxQuery
				{
					OnColumns = cube.WholesaleDollars,
					OnRows = cube.MostCurrentYears(2)
				};		
	}
	The above query will return the wholesale dollars for 2015, and 2014 (assuming that at least one transaction occured in the year 2015).


---- 2(B) - Object Methods - ----
The MDX language consists of many different functions.  Not every function is directly implemented in PAS (yet). For other functions not implemented, 
use the "Mdx.MdxFunction<T>()" method. The first parameter is the name of the function, and the rest of the parameters are the 
objects (in order) in which you want to pass to the MDX function.

The static "Mdx" class offeres a variety of methods that correspond to the MDX functions, for example Mdx.Sum() and Mdx.TopCount().  Each one of these
expects what is needed, whether is be a set, member, level, number, string expression, or what have you - and are type safe.  This way you know before 
you run the query if a method is expecting a different type durring the MDX query.  Each one of these methods also return the appropriate set or memeber, 
so these methods can be used to create larger sets or members.

Example => 
	Member myMember = Mdx.Sum(db.Stores.StoreState, db.WholesaleDollars); // Amounts to "Sum([Stores].[StoreState], MEASURES.[Wholesale Dollars])"

////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////
3 - QUERIES																							  //
////////////////////////////////////////////////////////////////////////////////////////////////////////
---- - 3(A) USING STANDARD LINQ QUERY SYNTAX - ----

PAS implements "LINQ to MDX" to query an Analysis Services Database.  Use the "where" clause to introduce a "WHERE" slicer into the MDX query,
and use the select clause to select members and sets for the query.  The "Select" method requires selecting a "MdxQuery" object and populating
its properties with the contents of the desired axes.

The LINQ sytanx is for simpler queries that don't involve much more than just asking for some dimensions on some axes.

Simple LINQ example =>
	var mdx = from cube in db.MyCube				
			  where cube.Calendar.Year["2014"]                           
			  select new MdxQuery					 
			  {														     
			      OnColumns = cube.WholesaleDollars & cube.TransactionCount,
				  OnRows = cube.Stores.StoreID.This * cube.Stores.StoreName.This		
			  };														 
			
	will translate to => 															 
		SELECT
		{
		    MEASURES.[Wholesale Dollars]
		} ON COLUMNS,
		{
		    [Stores].[Store ID].[Store ID] *
			 [Stores].[Store Name].[Store Name]
		} ON ROWS
		FROM [MyCube]
		WHERE
		(
		    [Calendar].[Year].[2014]
		)																 
																		 
In the above example, we see a slicer for calendar year 2014, and two axes being selected. 
More examples and ways to use the Mdx methods and building members and sets in the examples
section.

To add multiple sets or members to the WHERE slicer, either add more "where" clauses to the LINQ query, create a set, or join the 
objects with the "&&" (exclusive and) operator.

Refer to seciton 4 for maping results to an object.

**As of 0.3, this LINQ syntax has changed. As of now, anonymous object cannot be mapped to.

Since generating a table for every possible option returned from an MDX query would be rediculous, it is up to the user to create their own objects
for the query to map to. It's important to use properties in a class you have made to map to, as other members
will not be recognized.  Refer to section 4 for mapping.

---- - 3(B) USING PAS EXPLICIT SYNTAX - ----

As of version 0.2.* ->
The PAS explicit syntax, well, gives a more explicit control over quering the cube to the user.  The PAS explicit syntax allows the user to decide what
axis they want what members on, and allows the maximum amount of possible axes to select on. This syntax supports complex queries with almost all
syntax supported.

**	As of 0.2 a max of two axes can be selected when using the result to map to an object.  If one only wishes to return the 
	CellSet itself, then more than two can be used.  In future release, mapping from more than two axes will be possible.

So without further adieu...

using(var db = new MycubeDatabase())
{
	var query = db.SalesCube.OnAxis(0, x => x.WholesaleDollars)
		.OnAxis(1, true, x.Stores.StoreName.Children)
		.OnAxis(2, x => x.Stores.StoreID.Children * x.Stores.StoreAddress.Children * x.Stores.StoreCity.Children)
		.Slice(x => x.Calendar.Year["2015"])
		.FromSubCube(x => x.Stores.Status["Active"])
		.GetCellSet();
}
	the above will generate =>
	SELECT
	{
		Measures.[Wholesale Dollars]
	} ON 0
	,
	NON EMPTY
	{
		[Stores].[Store Name].Children
	} ON 1
	,
	{
		[Stores].[Store ID].Children * [Stores].[Store Address].Children * [Stores].[Store City].Children
	} ON 2
	FROM 
	(
		SELECT 
		{
			[Stores].[Status].["Active"]
		} ON 0
		FROM [Sales Cube]
	)
	WHERE 
	(
		[Calendar].[Calendar Year].[2015]
	)

	**	Notice here that there are three axes queried and also notice the overloaded "OnAxis" method for the "1" axis specifies
		an overloaded "true".  This boolean indicates a "NON EMPTY" axis as seen in the result.  Also notice the overloaded
		"*" symbol to cross join the three sets in the third axis. 

	For those of you who like to use the named axes there are five static members from the non generic "Axis" class that can
	be used to specify the axis you want, rather that using a number. Also as with the "*" operator, the "&" operator is overloaded
	to signify joining two objects to creat a comma seperated tuple.

	An example =>
		db.SalesCube.OnAxis(Axis.COLUMNS, x => x.TransactionCount & x.WholesaleDollars)

	The above axis example will translate to

	{
		(Measures.TransactionCount, Measures.WholesaleDollars)
	} ON 0

	Also, passing in an IEnumerable<ICubeObject> will result in a comma seperated collection.

	An example =>
		db.SalesCube.OnAxis(Axis.COLUMNS, x => new [] {x.TransactionCount, x.WholesaleDollars})

	This will translate to 
	{
		Measures.TransactionCount
	,	Measures.WholesaleDollars
	} ON 0

	More example are listed in the "Examples" section.  For mapping these queries, refere to section 4(A) - Using User Created Objects.
}

---- - 3(C) USING THE GOOD OL' STRING QUERY - ----

Simply using a string query is available as well using the Execute(string query) methods. The results will be returned in a data table.

	var mdxString = "SELECT Measures.[Wholesale Dollars] ON 0, [Stores].[Store Name].[Store Name] ON 1 FROM [Sales Cube]"
	DataTable mdxTable = db.Execute(mdxString);

	OR

	var myObject = db.Percolate<MyObject>(mdxString);

////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////
4 - MAPPING																							  //
////////////////////////////////////////////////////////////////////////////////////////////////////////

PAS provides a data mapper for both normal objects and anonymous objects. Since there are almost infinite possibilities of table combinations,
it would be crazy to try to use a T4 template to generate every possible table for an MDX query. So (for now at least) creating the 
db objects is left up to the user.

---- 4(A) - USING USER CREATED OBJECTS WITH LINQ FREE FORM SYNTAX - ----

The Linq usage for PAS is a bit different than most SQL or IEnumerable Linq ideas.  The Idea here is to define a where slicer (if you wish)
and then select an "MdxObject" that defines what your axes will look like. What happens internaly is your axes selection are stored in the cube
object and then used when you call an execute method or the Percolate method (similar to PAS syntax).

	public class StoreDollarsByItem
	{
		[MapTo("Wholesale Dollars")]
		public double Dollars { get; set; }

		[MapTo("Store Name")]
		public string Name { get; set; }

		[MapTo("Store ID")]
		public int ID { get; set; }
	}

	var query = from cube in db.MyCube
				where cube.Calendar.Year["2014"]
				where cube.Items.ItemID["&01652"]
				select new MdxQuery
				{
					OnColumns = cube.WholesaleDollars,
					OnRows = cube.Stores.StoreName.This * cube.Stores.StoreID.This
				};

	var results = query.Percolate<StoreDollarsByItem>();

**	The name assigned to the "MapToAttribute" must match the caption name of the column from the query. 
	If you are seeing empty values where you know there should be something, chances are, the name of the MdxColumn
	you gave does not match any of the column caption names returning from the CellSet.  

**	Any columns not tagged with the "MapTo" attribute will be ignored, so if your object has other properties in it that are used
	for other things, they will not be touched.

---- 4(B) - USING ANONYMOUS OBJECTS - ----

**As of version 0.3, the anonymous mapping has been taken out.  I am going though a few ideas of how I may re-implement this.

---- 4(B) - USING THE PERCOLATE METHOD WITH PAS EXPLICIT SYNTAX - ----

As of version 0.2, mapping to a user created object is now possible via the "Percolate" method. I will use an example
from the above section "3(B) USING PAS EXPLICIT SYNTAX", and an example of a class to hold the values returned from the query.

//Here is our class =>
public class StoreInfo
{
	[MapTo("Transaction Count")]
	public double TranCount { get; set; }

	[MapTo("Wholesale Dollars")]
	public double WholesaleDollars { get; set; }

	[MapTo("Store Name")]
	public string Name { get; set; }

	[MapTo("Store Address")]
	public string Address { get; set; }

	[MapTo("Store ID")]
	public int ID { get; set; }
}

//Here is our query
//Since as of version 0.2 we can only map from queries with no more than two axes,
//I placed all my store info in the second axis.
var storeInfo = db.SalesCube
		.OnAxis(0, x => x.WholesaleDollars)
		.OnAxis(1, true, x => x.Stores.StoreName.This * x.Stores.StoreID.This * x.Stores.StoreAddress.This * x.Stores.StoreCity.This)
		.Slice(x => x.Calendar.Year["2015"])
		.FromSubCube(x => x.Stores.Status["Active"])
		.Percolate<StoreInfo>();

And bam, your done.  Once the query is successfull, the percolate method will return your results in an IEnumerable of the type you provided.
In our case, we will have an IEnumerable of type "StoreInfo".

**	The name assigned to the "MapToAttribute" must match the caption name of the column from the query. 
	If you are seeing empty values where you know there should be something, chances are, the name of the MdxColumn
	you gave does not match any of the column caption names returning from the CellSet.  

**	Any columns not tagged with the "MapTo" attribute will be ignored, so if your object has other properties in it that are used
	for other things, they will not be touched.


////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////
5 - Examples																						  //
////////////////////////////////////////////////////////////////////////////////////////////////////////
---- - 5(A) MDX Functions as PAS Methods - ----

These are examples of some working methods =>

//Finding the Top Ten Selling items using a previously created aggregated member "TwelveMonthTotal"
Mdx.Order(
	Mdx.TopCount(
		SalesCube.Objects.Items.ItemID.Children, 
		10, TwelveMonthTotal), 
	TwelveMonthTotal, 
	OrderType.BDESC);

//Filtering locations by locations selling at least one item
//Use a parameterless lambda expression for the conditional expression
Mdx.Filter(SalesCube.Objects.Locations.LocationID, () => SalesCube.Objects.SWPDollars > 0);

//Year to Date and prior year to date
Mdx.Sum(
    Mdx.PeriodsToDate(
		db.MyCubeObjects.Calendar.CalendarbyWeek.Year,
		Member.Create<SalesCube>(x.Calendar.MonthID["4154"], x.Calendar.CalendarbyWeek.Month).Function("Item(0).Item(1)")),
    db.MyCubeObject.SWPDollars);

Mdx.Sum(
        Mdx.PeriodsToDate(
			SalesCube.Objects.Calendar.CalendarbyWeek.Year,
            Mdx.ParallelPeriod(
				SalesCube.Objects.Calendar.CalendarbyWeek.Month, 
				12, 
				Member.Create<SalesCube>(x => x.Calendar.MonthID[monthId], x.Calendar.CalendarbyWeek.Month).Function("Item(0).Item(1)"))),
        db.MyCubeObject.SWPDollars);

---- - 5(B) CREATING QUERY SCOPED CALCULATED MEMBERS AND SETS - ----
You can create query scoped members and sets before assembling a query, and then pass them into the query to use them.

Creating members and sets =>
	using (var db = new MyCubeDatabase())
	{
		var dollars = SalesCube.Objects.WholesaleDollars // using a static reference to the objects in a cube called "SalesCube"

		var valueByYear = GetMeasureByYear(dollars, 2014); 
	
		Set mySet = Set.Create<SalesCube>(x => x.Stores.StoreID * x.Calendar.Month);
	
		Member storesRating = Mdx.IIf(() => db.ReSightObjects.CustDollars > 5000, "GOOD", "POOR", HINT.None, HINT.None);
	
	//Using the "|" to represent the MDX ":" (between) operator =>
		string currentMonthId = "4352"
		var setExample = SalesCube.Objects.Calendar.MonthID[currentMonthId].Function("Lag(2)") | SalesCube.Objects.Calendar.MonthID[currentMonthId];
	}

	//Or to create a method that returns a certain measure or member depending on the parameter passed =>
	
	public Member GetMeasureByYear(string type, int year)
	{
		var sales = new SalesCube();
		switch(type)
		{
			case "Wholesale":
				return new Member(sales.WholesaleDollars, sales.Calendar.Year[year]);
	
			case "Retail":
				return new Member(sales.RetailDollars, sales.Calendar.Year[year]);
	
			case "CustomerReported":
				return new Member(sales.CustomerDollars, sales.Calendar.Year[year]);
	
			default:
				throw new ArgumentException("The measure type is not recognized.");
		}
	}
	

//Example for global query scoped calculated members and sets =>
	publilc partial class MyCube
	{
		public Set TopTenItems {get { return Mdx.TopCount(this.Items.ItemName.Children, 10, this.WholesaleDollars); } }
	}

//Other Example using strings =>
	You can also pass in a string representation of a member you wish to create in code:

		var year2014Dollars = new Member("([Calendar].[Year].&[2014], Measures.[Wholesale Dollars])");

	or using implicit conversion ->

		Member year2014Dollars = "([Calendar].[Year].&[2014], Measures.[Wholesale Dollars])";

	* Same thing applies to Sets

---- 5(C) - QUERYING USING LINQ STANDARD SYNTAX - ----

// Simple query
	using (MyCubeDatabase db = new MyCubeDatabase())
	{
		string year = "2014";			
		string itemId = "90382"				
		var query = from cube in db.MyCube				
					where cube.Calendar.Year[year]		
					where cube.Items.ItemID[itemId]
					select new MdxQuery		
					{									
						OnColumns = cube.WholesaleDollars,			
						OnRows = 
							cube.Stores.StoreName.This *	
							cube.Stores.StoreID.This *
							cube.Stores.Manager.This
					};									
	}													
														
	translates to => 
					SELECT
					{
					    MEASURES.[Wholesale Dollars]
					} ON COLUMNS,
					{
					    [Stores].[Store Name].[Store Name] *
					    [Stores].[Store ID].[Store ID] *
						[Stores].[Manager].[Manager]
					} ON ROWS
					FROM [MyCube]
					WHERE
					(
						[Calendar].[Year].[2014],
						[Items].[Item ID].[90382]
					)		

**NOTE ->	To add multiple sets or members to the WHERE slicer, either add more "where" clauses to the LINQ query (as above), create a set, or join the 
			objects with the "&&" (exclusive and) operator.

// Query slicing by a set
	using (MyCubeDatabase db = new MyCubeDatabase())
	{
		string itemId = "1938";
		string managerId = "MWRM"

		Set whereSlicer = new Set(MyCube.Objects.Items.ItemID[item1Id], 
								  MyCube.Objects.Stores.ManagerID[managerID]);

		var query = from cube in db.MyCube					
					where whereSlicer						
					where cube.Calendar.Year["&2014"]		
					select new MdxQuery
					{										
						OnColumns = 
							cube.WholesaleDollars &				
							cube.RetailDollars &					
							cube.TransactionCount,
						OnRows = 				
							cube.Stores.StoreID.This *		
							cube.Stores.StoreName.This *		
							cube.Items.ItemName.This		
					};										
	}														
		
	translates to => SELECT
					 {
				         MEASURES.[Wholesale Dollars], 
						 MEASURES.[Retail Dollars],
						 MEASURES.[Transaction Count]
			         } ON COLUMNS,
					 {
					     [Stores].[Store ID].[Store ID] *
						 [Stores].[Store Name].[Store Name] *
						 [Items].[Item Name].[Item Name]
					 } ON ROWS
					 FROM [ReSight]
					 WHERE
					 (
						 {[Items].[Item ID].[1938] * [Stores].[Manager ID].[MWRM]},
						 [Calendar].[Year].&[2014]
					 )																			
															
	** NOTE -> It's important to note that in this case, the previously created set "whereSlicer" does not appear as a query scoped 
			   calculated set, which is then placed in the where clause, but is instead pulled apart and the set is placed directly 
			   in the "WHERE" clause.  This is because a where slicer cannot contain calculated sets or members - it creates a
			   circular dependancy in the query on the server.

// Query using MDX functions
	using (MyCubeDatabase db = new MyCubeDatabase())
	{
		var query = from cube in db.MyCube
					where Mdx.Head(Mdx.Order(cube.Calendar.Year, cube.Calendar.Year.Name, OrderType.BDESC), 1)
					select new MdxQuery
					{
						OnColumns = Mdx.Sum(cube.Stores.StoreState.Children, cube.CustomerDollars),
						OnRows = 
							Mdx.Filter(cube.Stores.StoreID.Children, () => cube.Stores.StoreCountry == "US") *
							cube.Stores.StoreName.This *
							cube.Stores.ManagerID.This
					};
	}
		translates to => 
						SELECT
						{
						    Sum([Stores].[Store State].Children, MEASURES.[Customer Dollars])
						} ON COLUMNS,
						{
						    Filter([Stores].[Store ID].Children, [Stores].[Store Country] = "US") *
							[Stores].[Store Name].Children *
							[Stores].[Manager ID].Children
						} ON ROWS
						FROM [MyCube]
						WHERE
						(
							Head(Order([Calendar].[Year], [Calendar].[Year].Name, BDESC), 1)
						)
	
---- 5(D) - QUERYING USING PERCOLATOR EXPLICIT METHODS - ----				

Using this object =>
public class ItemInfo
{
	[MapTo("Item ID")]
	public int ID { get; set; }

	[MapTo("Long Item Name")]
	public string LongName { get; set; }

	[MapTo("Short Item Name")]
	public string ShortName { get; set; }

	[MapTo("Wholsale Dollars")]
	public double WholesaleDollars { get; set }

	[MapTo("Retail Dollars")]
	public double RetailDollars { get; set; }

	[MapTo("Transaction Count")]
	public double TransactionCount { get; set; }

	public double GrossMargin { get { return this.RetailDollars - this.WholesaleDollars; } }
}

using(var db = new MycubeDatabase())
{
	//Example of top ten selling items
	var topTenItems = db.SalesCube
		.OnAxis(0, x => new [] { x.WholesaleDollars, x.RetailDollars, x.TransactionCount })
		.WithSet("ItemDetails", 1, 
			x => Mdx.TopCount(x.Items.ItemID.Children * x.Items.LongItemName.Children * x.Items.ShortItemName, 10, x.TransactionCount)
		)
		.Slice(x => x.Calendar.Year["2015"] & x.Stores.StoreState["CA"])
		.Percolate<ItemInfo>();
}

	The above query will result in ->
	SELECT
	{
		Measures.[Wholesale Dollars]
	,	Measures.[Retail Dollars]
	,	Measures.[Transaction Count]
	} ON 0
	,
	{
		TopCount({[Items].[Item ID].Children * [Items].[Long Item Name].Children * [Items].[Short Item Name].Children}, 10, Measures.TransactionCount)
	} ON 1
	FROM [Sales Cube]
	WHERE
	(
		[Calendar].[Year].[2015]
	,	[Stores].[State].[CA]
	)

** NOTE -	Notice there is not a spcific OnAxis call for the second or "1" axis.  The translator saw you were creating a Set and wished to call it
			on the "1" axis, so it created an axis and added that set name into the axis call.  If you had called an OnAxis call for axis "1",
			the created set will be cross joined to the rest of the axis contents.  A member will be added by a comma seperation.

	And FYI - the order in which you call the methods doesn't matter. The same query above could be =>
	var topTenItems = 
		db.SalesCube
		.Slice(x => x.Calendar.Year["2015"] & x.Stores.StoreState["CA"])
		.WithSet("ItemDetails", 1, 
			x => Mdx.TopCount(x.Items.ItemID.Children * x.Items.LongItemName.Children * x.Items.ShortItemName, 10, x.TransactionCount)
		)
		.OnAxis(0, x => new [] { x.WholesaleDollars, x.RetailDollars, x.TransactionCount })
		.Percolate<ItemInfo>();

		- so long as the Percolate method is called last.

This example is a more in depth query that I have used in previous projects (with some members changed), along with its object to map to. =>

	//These varibles were passed in through the method that calls this query. For the sake of this example, I have
	//hard coded its value.
	var monthId = "&3054";
	var distId = "AALF";

    var month = Sales.Objects.Calendar.MonthID[monthId];
    var monthHierPair = Member.Create<Sales>(x => (x.Calendar.MonthID[3054] & x.Calendar.CalendarbyWeek.Month).Item(0).Item(1));

	db.Sales
    //First Axis (Columns)
    .WithMember("Measures.TwelveMonthTotal", Axis.COLUMNS, x => Mdx.Aggregate(month | month.Lag(11), x.SWPDollars))
    .WithMember("Measures.ThreeMonthAverage", Axis.COLUMNS, x => Mdx.Average(month | month.Lag(2), x.SWPDollars))
    .WithMember("Measures.CurrentMonthTotal", Axis.COLUMNS, x => month & x.SWPDollars)
    .WithMember("Measures.PriorMonthTotal", Axis.COLUMNS, x => month.Lag(1) & x.SWPDollars)
    .WithMember("Measures.CurrentMonthYTDTotal", Axis.COLUMNS, x => Mdx.Sum(Mdx.PeriodsToDate(x.Calendar.CalendarbyWeek.Year, monthHierPair), x.SWPDollars))
    .WithMember("Measures.PriorYTDTotal", Axis.COLUMNS,
        x => Mdx.Sum(
            Mdx.PeriodsToDate(
                x.Calendar.CalendarbyWeek.Year,
                Mdx.ParallelPeriod(x.Calendar.CalendarbyWeek.Month, 12, monthHierPair)),
            x.SWPDollars))
    .OnAxis(Axis.COLUMNS, true, x => x.TransactionCount)

    //Second Axis (Rows)
    .OnAxis(Axis.ROWS, x =>
        Mdx.Order(
            Mdx.NonEmptyCrossJoin(
                Mdx.TopCount(
                    x.Items.Code.Children,
                    10,
                    Mdx.Aggregate(monthHierPair.Lag(11) | monthHierPair, x.SWPDollars))),
            x.SWPDollars,
            OrderType.BDESC
    ) * x.Items.ShortItemName.Children)
    
    .Slice(x => x.Distributors.DistID[distId] & x.Items.Status["&Active"])
    .Percolate<TopTenChart>()
    .ToList()
    .ForEach(Console.WriteLine);

	//The object
	public class TopTenChart
    {
        [MapTo(MdxColumn = "Short Item Name")]
        public string FunTime_Name { get; set; }

        [MapTo(MdxColumn = "TwelveMonthTotal")]
        public double TwelveMonthTotal { get; set; }

        [MapTo(MdxColumn = "ThreeMonthAverage")]
        public double ThreeMonthAverage { get; set; }

        [MapTo(MdxColumn = "CurrentMonthTotal")]
        public double CurrentMonthTotal { get; set; }

        [MapTo(MdxColumn = "PriorMonthTotal")]
        public double PriorMonthTotal { get; set; }

        [MapTo(MdxColumn = "CurrentMonthYTDTotal")]
        public double CurrentMonthYTD { get; set; }

        [MapTo(MdxColumn = "PriorYTDTotal")]
        public double PriorYTDTotal { get; set; }

        [MapTo(MdxColumn = "Code")]
        public string Code { get; set; }

        [MapTo(MdxColumn = "Transaction Count")]
        public double TranCount { get; set; }

        public override string ToString()
        {
            var str = string.Format("[Item ID = {0}] : [Name = {7}]\r\n\t[TwelveMonthTotal = {1}]\r\n\t[ThreeMonthAvg = {2}]\r\n\t[CurrentMonthTotal = {3}]\r\n\t" +
                        "[PriorMonthTotal = {4}]\r\n\t[CurrentYTD = {5}]\r\n\t[PriorYTD = {6}]\r\n\t[Tran Count = {8}]",
                        this.Code, this.TwelveMonthTotal, this.ThreeMonthAverage, this.CurrentMonthTotal, 
                        this.PriorMonthTotal, this.CurrentMonthYTD, this.PriorYTDTotal, this.FunTime_Name, this.TranCount);
            return str;
        }
    }

	Notice again here that there is not explicit OnAxis method call for the first (0) axis, but there were members that were created
	and specified to appear on the first axis.

	**NOTE ->	If no axis is specified in a "WithMember" or "WithSet" call then the member or set will be created, but not placed in
				an axis to be queried.  This is usefull at times where you wish to create a member fo example that you use to 
				assemble other members or sets further on in the query, but you don't wish to query that member's value.

////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////
6 - THINGS TO KNOW																					  //
////////////////////////////////////////////////////////////////////////////////////////////////////////

**FIRST OF ALL - Thanks to everyone for their support! Please, don't forget, if you have a bug or an issue LET ME KNOW! 
				 I can't help if I don't know the problem.  You can contact me on the project's NuGet page via the 'Contact Owners' link.

**SECOND OF ALL -	Once I feel that this library is strong enough and bug free enough I will pull it out of beta and make the full release as 
					well as open sourcing it (most likely on code plex or git hub).  If you wish to have the source as of right now (beta),
					please contact me and I will get back to you as soon as possible.

1)	This is a product under testing and development, and carries no waranty of any kind. 

2)	Not everything in MDX is implemented yet. If we get enough people asking for more, we will give them more (working on more because people
	as asking for it!).

3)	This is a product built from a want for a more dynamic/strong typed way of querying mdx for our little company. Again, if there is something
	we dont have implemented it's probably because we don't use it too often ourselves, so let us know.

4)	In regards to the AdomdDataReader that is used to map to objects in the LINQ queries, this reader does NOT return any column for an "All" type member.  
	That is to say, if an all memeber is used, the mapper will blow up in your face, of which of course we hold no warranty to. 
	Also, using just the attribute in the free form LINQ syntax will be translated into using the members of that attribute => select new { db.Stores.StoreName }
	will tanslate to [Stores].[Store Name].[Store Name]. More explicitly, using the ".Children" property will work as well.  However, the PAS
	explicit syntax uses a CellSet object, so "All" members are returned, and the extra member tag will NOT be implicitly included.  Remember,
	PAS explicit syntax is explicit (hence the name).

5)	In regards to mapping to an object with the LINQ syntax, mapping to an actual object is faster and lighter than mapping to an anonymous object. 
	Chances are, you wont see much of a difference, but a slight difference is there.  Also, only the properties of the object that you specify in 
	the select statement will be mapped to.  Any other properties will be ignored, as well as any other fields. Using the "Tag" attribute, 
	or any other attribute is not needed.

6)	Only properties will be mapped to in an object, no other member types.

7)	When using one of the 'GetDataReader' or 'ExecuteDataReader' methods, keep in mind that the reader is an open connection, and should be disposed of
	properly, or enclosed in a 'using' statement.

8)	In regards to sub cubes, the LINQ or PAS queries that you pass into a new sub cube isn't ran against the database.  Instead, the query
	is translated into the appropriate MDX string and placed into the "FROM" statement of the main query it belongs to. Therefore, no real 
	values are obtainable through a subcube. 

9)	Working with the LINQ free form syntax requires working around certain compiler restrictions, seein that it was built
	for database queries rather than multi dimensional queries, especially when it comes to mapping objects.  With this in mind, 
	certain things don't work as expected, and some features either are too difficult to maintain or just can't be done.  
	Threrefore, further developent and features will most likely not continue with this syntax.  

	The LINQ free form syntax was put in place for simple, easy queries where the user doesn't really care what goes on behind the scenes. If 
	you want more explicit control of how the query is structured, the PAS explicit syntax is what you want. 

10) Some objects used in this library come from the Microsoft.AnalysisServices.AdomdClient dll provided by Microsoft. In order to use
	these objects (CellSet, AdomdDataReader, etc) you must use that dll in your project as well.  Simply using the built in mapping
	doesn't require the dll.

11) Overloaded operators =>
		| (Pipe operator) - used as the range operator (:) in MDX.
		* (Multiply Operator) - used as the Shorthand Cross Join operator (*) in MDX.
		& (Bitwise AND Operator) - denotes joining members and sets into a comma seperated tupled set or member.

12) Yes, the Linq implementations have been stripped back quite a bit. Honestly, they were crappy and only worked half the time, and the IQueryable 
	interface implementation got messy.  It was a huge bloated thing and I was tired of looking at it. If there happens to be an outcry of people wanting 
	the old stuff back the I may oblige, but I doubt that will happen.  The feedback witht he new PAS explicit syntax from 0.2 is very good and people
	tended to use that more anyways. Really, the Linq syntax was meant for light, quicker queries in the first place anyways. I still am working on
	different ideas of how to include anon objects and they may come soon.  If you have opinions on this please let me know. Thanks.

13) The "This" property has been added to the Attribute class.  For example, cube.Stores.StorID.This will translate to "[Stores].[Store ID].[Store ID]".
