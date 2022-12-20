# Generative-Grammar-for-NPCs
<h2>Grammar Structure</h2>
<p style="text-align: justify">There are several convention set to make the parsing of the grammar easier. 
For example, the following node <b>A := B ~ C</b> means that from node <b>A</b> we expand to both nodes <b>B</b> and <b>C</b>.
In the case of <b>A := B | C</b>b it means that only one node has to be picked between <b>B</b> and <b>C</b>.
Moreover, every node of the form <b>A := B | C</b> must have the weights of choosing either node <b>B</b> or <b>C</b> written like this:</p> 

<div style="text-align: center"><b>A := [WB] B | [WC] C</b></div>

<p style="text-align: justify">Every node can have attributes attached to the nodes it expands to, separated by a colon. 
For example:</p>
<div style="text-align: center"><b>A := B : b <- 1 ~ C : c <- 2</b> <br>or<br> <b>A := B ~ C : b <- 1, c <- 2</b><br></div>
<p style="text-align: justify">The attributes can be written after every node and in any order and if there are more attributes written after a node they have to be separated by a comma. 
Also, if the node if of the form <b>A := B | C</b> and attributes are set only on the side of <b>B</b> then if <b>C</b> is picked they will not be taken into consideration.
Using inline if statements is also supported for assigning values to attributes. One such example is the following:</p>
<div style="text-align: center"><b>A := B : condition ? b <- 1 : b <- 2</b></div>
<h2>Augments</h2>
<p style="text-align: justify">There are 3 types of augments added to aid in the generation of NPCs</p>
<list style="text-align: justify">
<li><b>from</b> which indicates the source for the node's possible values.</li>
<li><b>condition</b> where a series of conditions are defined and they have to be tru in order for the generation to be correct.</li>
</list>
<h2>Type of Nodes </h2>
<list style="text-align: justify">
<li><b>Terminal node</b> - type of node that has no following nodes</li>
<li><b>Non-terminal nodes</b> - any other node which can be expanded to new nodes</li>
<li><b>Source node</b> - while these nodes can be either terminal or non-terminal they are considered terminal nodes, because they take value from a source file.
If the node is non-terminal then the following nodes have to be terminal and they are used to filter the list of elements from the source file.</li>
</list>
<h2>Assumptions & Decisions</h2>
<list style="text-align: justify">
<li>Source nodes are either terminal or non-terminal and all the following nodes are terminal.</li>
<li>Because the game used is Pokemon the only supported data types are int, boolean and string.</li>
<li>The only supported functions are MIN, MAX, SIZE and DISTINCT.</li>
<li><b>TYPE.DamageTaken</b> function is a custom method and is hard coded, because of the difficulty of implementation in a general way.</li>
<li>If <b>=> (imply)</b> is used in a condition, it is used only once and is always of the form <b>A => B</b>, where A and B are boolean euqations that have no imply</li>

</list>