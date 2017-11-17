class Player < ApplicationRecord
	belongs_to :clan
	has_many :marks
	
	def to_param
		name
	end
end
