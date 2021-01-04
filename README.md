# Noway
Noway is a completely custom programming language that could change the way we code

# Noway features:
<ul>
<li>Noway infers almost everything, unless explicitly stated otherwise, allowing you to make your code more concise</li>
<li>Noway offers a lot of syntax sugar, making it really simple to do complex tasks</li>
<li>Noway is staticly typed, allowing more errors to be caught at compile time</li>
<li>Noway offers a standard library of all the basic functions you would want in any programming language (in progress)</li>
<li>Noway is compiled, but I will also create an interpreter (DotNet based)</li>
<li>Noway is cross-language, cross-platform</li>
</ul>

# Why did I create Noway?
I've wanted to build a programming language for years, and had many failed attempts, but this one works.
Over the years I've gotten a bit older and wiser with regards to issues in current programming languages and their corrosponding solutions, but one thing sticks out:

Why do we have so many programming languages?

Because programming languages have certain strengths and weaknesses just by their design, and by their speed and influence in certain spheres of coding.

What if we had access to all the strengths, and ability to avoid weaknesses? It might sound insane, but that's where Noway comes in.
Noway doesn't get compiled to machine code, instead it is compiled to the source code of other common languages, (c++, c, java, c#, python)
and you will be able to flip between languages, and use the full power of each language anywhere in the code.

Here is an example of the utilization of 3 different programming languages in the Noway programming language
```cs
Program {
  /*
    A little about method syntax:
    
    Prefixes:
    +, and - before methods indicate whether they are public or private
    
    a method prefixed by an underscore means it is utilized in a special way by the compiler:
    _main is for the program entry points
    _new is for constructors
    _add, _sub, _div, _equals, etc. are for operator overloads
    _toStr is for conversion to a string
    _cast is for explicit casting
    _convert is for implicit casting
    _getIndex for indexers (variable[0])
    _setIndex for indexers (variable[0] = a)
    
    the return type of a method is infered from the last expression of the method, and if the type is not void, the result is returned
    the argument types can be explicitly stated if the compiler cannot figure it out (which is rare)
    */
    
  +_main(args) { //args is infered to be a sys.string[], because it is the signature for main methods
  
     //use standard system library for Noway (can compile to any language)
     sys.console.println("hello world")
     
     /*
       the hashtag is a compiler directive to allow the use of c# libraries,
       this part of the code will get compiled to a c# program
       that will interact with the Noway host on execution
       */
     #cs
     System.Console.WriteLine("hello world")
     
     /*
       do the same but for java,
       now we will have both a c# and a java program that are compiled
       and they will both interact with the Noway host on execution
       */
     #java
     //use of c# libraries are now disallowed otherwise it wouldn't really work
     System.out.println("hello world")
     
     #python
     //...and python
     print("hello from python")
  }
}
```
Regardless of the what language is active, it has no impact on the syntax of the language, it only affect what methods and types you have access to.
In the future there will be a background type conversion system so types created in Noway can be translated between languages seamlessly as shown in the following code:
```cs
Vector { //create type Vector
   int x, y
   +_new(x, y) { //this defines a public (+) constructor (_new)
      .x = x
      //.x is the same as this.x in c# or java syntax.
      .y = y
      //x and y are inferred to be integers because they are being set to the value of an intege
   }
   -_add(Vector other) { //this defines a private (-) operator overload for + (_add)
      /*
        private for these methods means that the method is not visible for other types
        vec._add(vec2) is not valid as _add isn't public, but vec + vec2 is valid, because the operator overload is still defined
        */      
   
      Vector(other.x + .x, other.y + .y)
      //the return type is also inferred. the last expression is of type Vector, so the method returns that Vector
   }
   +_toStr() {
      "<"+.x+","+.y+">"
   }
}
Program {
   +_main(args) {
      v1 = Vector(4, 5)
      
      #cs
      v2 = Vector(3, 6)
      System.Console.WriteLine(v1) //prints out <4,5>
      
      #python
      v2 += v1;
      print(v2) //prints out <7,11>
   }
}
```
Since the Vector type was created in Noway, the compiler knows how to translate it from Noway to other languages,
consequently, it can also figure out how to a convert a Noway type created in a c# program to one in python
This is going to be part of what I call the inter-language pipeline, which causes the seperately generated programs to work using shared memory,
(using memory mapped files or some other technology) allowing data that needs to be transfered between languages to be stored in a common manner,
allowing different languages to use it seamlessly.

#  What this can do for coding
This has the possibility to revolutionize the way people code. No longer will development teams need to have seperate teams working on seperate parts of a project.
No longer will it be difficult to incorporate multiple languages in one project. Have a library in python for graphing? use it with a fast c++ backend in the same file!
Have some motor control libraries in java, but you have a great data analysis library in c#? use both in the same project!
Tired of ridiculous GPU shader functions and a lack of good intellisense? Noway will make GPU code easier to utilize alongside your CPU code!
Have a library in Noway? Use it anywhere, in any language (assuming it's supported).

Noway will eventually (gimme some time) have an official IDE, or vscode extension, with possible on-the-fly compilation to source code,
allowing errors to be found eariler, be more informative, and allow the compilation of the source code to the final product be that much quicker.
