# Leak-Free Functional Reactive Programming (FRP)

This is a C# implementation of Neelakantan Krishnaswami's simple, leak-free FRP interface:

http://semantic-domain.blogspot.ca/2015/07/higher-order-functional-reactive.html

The only change is that a fixed point on the Next type isn't provided, since it's simpler and
more efficient to simply Next.Delay with a thunk that invokes the calling method recursively.

One other notable difference that is possible in C#, but not in the original OCaml implementation,
is that the Stream and Event types are structs, ie. stack-allocated value types. This is possible
because they are immutable, and only the shared Next type is mutable. This enables a very compact,
efficient implementation of FRP.